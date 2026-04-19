using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Core.Services;
using ProjectApprovalSystem.Data;
using ProjectApprovalSystem.Data.Context;
using Xunit;

namespace ProjectApprovalSystem.Tests.Integration;

/// <summary>
/// Integration tests using in-memory database
/// Tests complete workflows with actual database
/// </summary>
public class BlindMatchingIntegrationTests : IAsyncLifetime
{
    private readonly PasDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlindMatchService _blindMatchService;

    public BlindMatchingIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<PasDbContext>()
            .UseInMemoryDatabase(databaseName: $"PAS_Test_{Guid.NewGuid()}")
            .Options;

        _context = new PasDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _blindMatchService = new BlindMatchService(_unitOfWork);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        await SeedTestData();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    private async Task SeedTestData()
    {
        // Create research areas
        var aiArea = new ResearchArea { Name = "Artificial Intelligence", Description = "AI/ML projects", IsActive = true };
        var webArea = new ResearchArea { Name = "Web Development", Description = "Web projects", IsActive = true };

        await _context.ResearchAreas.AddAsync(aiArea);
        await _context.ResearchAreas.AddAsync(webArea);

        // Create users
        var student = new User
        {
            Email = "student@example.com",
            FullName = "Student One",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Student,
            IsActive = true
        };

        var supervisor = new User
        {
            Email = "supervisor@example.com",
            FullName = "Dr. Smith",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Supervisor,
            IsActive = true
        };

        await _context.Users.AddAsync(student);
        await _context.Users.AddAsync(supervisor);
        await _context.SaveChangesAsync();

        // Assign supervisor expertise
        var expertise = new SupervisorExpertise
        {
            SupervisorId = supervisor.Id,
            ResearchAreaId = aiArea.Id,
            IsActive = true
        };
        await _context.SupervisorExpertises.AddAsync(expertise);

        // Create proposal
        var proposal = new Proposal
        {
            ProposalId = "2024-0001",
            StudentId = student.Id,
            Title = "Deep Learning for Image Recognition",
            Abstract = "Using deep learning techniques for image classification",
            Description = "This project explores CNN architectures",
            TechStack = "Python, TensorFlow, Keras",
            ResearchAreaId = aiArea.Id,
            Status = ProposalStatus.Pending
        };

        await _context.Proposals.AddAsync(proposal);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task CompleteBlindMatchingWorkflow_ShouldSucceed()
    {
        // ARRANGE & ACT & ASSERT
        var students = await _unitOfWork.Users.GetByStudentRoleAsync();
        var student = students.First();

        var supervisors = await _unitOfWork.Users.GetBySupervisorRoleAsync();
        var supervisor = supervisors.First();

        var proposals = await _unitOfWork.Proposals.GetStudentProposalsAsync(student.Id);
        var proposal = proposals.First();

        // Step 1: Supervisor views anonymous proposals (should not see student name)
        var anonymousProposals = await _blindMatchService.GetAnonymousProposalsForSupervisorAsync(supervisor.Id);
        Assert.NotEmpty(anonymousProposals);
        Assert.All(anonymousProposals, p => Assert.Null(p.Student));

        // Step 2: Supervisor expresses interest
        var match = await _blindMatchService.ExpressInterestAsync(proposal.Id, supervisor.Id, "Very interested");
        Assert.NotNull(match);
        Assert.Equal(MatchStatus.Interested, match.Status);
        Assert.Null(match.Proposal?.Student);

        // Step 3: Supervisor confirms match (now student identity should be revealed)
        var confirmedMatch = await _blindMatchService.ConfirmMatchAsync(match.Id);
        Assert.Equal(MatchStatus.Matched, confirmedMatch.Status);
        Assert.NotNull(confirmedMatch.MatchedAt);

        // Step 4: Verify proposal status is updated
        var updatedProposal = await _unitOfWork.Proposals.GetByIdAsync(proposal.Id);
        Assert.Equal(ProposalStatus.Matched, updatedProposal?.Status);
    }

    [Fact]
    public async Task MultipleInterests_ShouldAllowProposalToReceiveMultipleMatches()
    {
        var supervisors = await _unitOfWork.Users.GetBySupervisorRoleAsync();
        var proposals = await _unitOfWork.Proposals.GetPendingProposalsAsync();
        var proposal = proposals.First();

        // Assume there are at least 2 supervisors in test data
        // Create additional supervisor with same expertise
        var researchers = await _context.ResearchAreas.ToListAsync();
        var researchArea = researchers.First();

        var supervisor2 = new User
        {
            Email = "supervisor2@example.com",
            FullName = "Dr. Johnson",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Supervisor,
            IsActive = true
        };
        await _context.Users.AddAsync(supervisor2);

        var expertise2 = new SupervisorExpertise
        {
            SupervisorId = supervisor2.Id,
            ResearchAreaId = researchArea.Id,
            IsActive = true
        };
        await _context.SupervisorExpertises.AddAsync(expertise2);
        await _context.SaveChangesAsync();

        // Both supervisors express interest
        var match1 = await _blindMatchService.ExpressInterestAsync(proposal.Id, supervisors.First().Id);
        var match2 = await _blindMatchService.ExpressInterestAsync(proposal.Id, supervisor2.Id);

        Assert.NotNull(match1);
        Assert.NotNull(match2);

        var allMatches = await _unitOfWork.Matches.GetProposalMatchesAsync(proposal.Id);
        Assert.Equal(2, allMatches.Count());
    }

    [Fact]
    public async Task DeclineMatch_ShouldUpdateStatus()
    {
        var supervisors = await _unitOfWork.Users.GetBySupervisorRoleAsync();
        var proposals = await _unitOfWork.Proposals.GetPendingProposalsAsync();
        var proposal = proposals.First();
        var supervisor = supervisors.First();

        var match = await _blindMatchService.ExpressInterestAsync(proposal.Id, supervisor.Id);
        await _blindMatchService.DeclineMatchAsync(match.Id);

        var updatedMatch = await _unitOfWork.Matches.GetByIdAsync(match.Id);
        Assert.Equal(MatchStatus.Declined, updatedMatch?.Status);
    }

    [Fact]
    public async Task StudentCannotSeeOtherStudentProposals()
    {
        var students = await _unitOfWork.Users.GetByStudentRoleAsync();
        var student1 = students.First();

        var student1Proposals = await _unitOfWork.Proposals.GetStudentProposalsAsync(student1.Id);
        Assert.NotEmpty(student1Proposals);

        // All proposals should belong to student1
        Assert.All(student1Proposals, p => Assert.Equal(student1.Id, p.StudentId));
    }
}

/// <summary>
/// Integration tests for proposal workflow
/// </summary>
public class ProposalWorkflowIntegrationTests : IAsyncLifetime
{
    private readonly PasDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProposalService _proposalService;

    public ProposalWorkflowIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<PasDbContext>()
            .UseInMemoryDatabase(databaseName: $"PAS_Workflow_{Guid.NewGuid()}")
            .Options;

        _context = new PasDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _proposalService = new ProposalService(_unitOfWork);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        await SeedBasicData();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    private async Task SeedBasicData()
    {
        var area = new ResearchArea { Name = "AI", IsActive = true };
        var student = new User
        {
            Email = "student@test.com",
            FullName = "Test Student",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            Role = UserRole.Student,
            IsActive = true
        };

        await _context.ResearchAreas.AddAsync(area);
        await _context.Users.AddAsync(student);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task SubmitAndRetrieveProposal_ShouldWorkCorrectly()
    {
        var student = (await _unitOfWork.Users.GetByStudentRoleAsync()).First();
        var areas = await _unitOfWork.ResearchAreas.GetActiveAreasAsync();
        var area = areas.First();

        var proposal = await _proposalService.SubmitProposalAsync(
            student.Id,
            "ML Project",
            "Using ML for classification",
            "Full details here",
            "Python, TensorFlow",
            area.Id
        );

        var retrieved = await _proposalService.GetProposalAsync(proposal.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("ML Project", retrieved.Title);
        Assert.Equal(student.Id, retrieved.StudentId);
    }

    [Fact]
    public async Task UpdateProposal_ShouldModifyProposalDetails()
    {
        var student = (await _unitOfWork.Users.GetByStudentRoleAsync()).First();
        var areas = await _unitOfWork.ResearchAreas.GetActiveAreasAsync();
        var area = areas.First();

        var proposal = await _proposalService.SubmitProposalAsync(
            student.Id, "Original Title", "Original Abstract", "Details", "Tech", area.Id
        );

        var updated = await _proposalService.UpdateProposalAsync(
            proposal.Id, "Updated Title", "Updated Abstract", "New Details", "NewTech"
        );

        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated Abstract", updated.Abstract);
    }

    [Fact]
    public async Task WithdrawProposal_ShouldChangeStatus()
    {
        var student = (await _unitOfWork.Users.GetByStudentRoleAsync()).First();
        var areas = await _unitOfWork.ResearchAreas.GetActiveAreasAsync();
        var area = areas.First();

        var proposal = await _proposalService.SubmitProposalAsync(
            student.Id, "Title", "Abstract", "Details", "Tech", area.Id
        );

        await _proposalService.WithdrawProposalAsync(proposal.Id);

        var withdrawn = await _proposalService.GetProposalAsync(proposal.Id);
        Assert.Equal(ProposalStatus.Withdrawn, withdrawn?.Status);
    }
}
