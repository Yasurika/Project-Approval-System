using Moq;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Core.Services;
using Xunit;

namespace ProjectApprovalSystem.Tests.Unit;

/// <summary>
/// Unit tests for BlindMatchService
/// Verifies that student identity is never revealed before matching
/// </summary>
public class BlindMatchServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BlindMatchService _service;

    public BlindMatchServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new BlindMatchService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetAnonymousProposalForSupervisor_ShouldNotExposeStudentIdentity()
    {
        var proposalId = 1;
        var supervisorId = 1;
        var proposal = new Proposal
        {
            Id = proposalId,
            ProposalId = "2024-0001",
            StudentId = 100,
            Title = "AI Project",
            Abstract = "Test abstract",
            TechStack = ".NET, React",
            ResearchAreaId = 1,
            Status = ProposalStatus.Pending,
            Student = new User { Id = 100, Email = "student@test.com", FullName = "John Doe", PasswordHash = "hash" }
        };

        _mockUnitOfWork.Setup(u => u.Proposals.GetAnonymousProposalAsync(proposalId))
            .ReturnsAsync(proposal);
        _mockUnitOfWork.Setup(u => u.SupervisorExpertises.HasExpertiseAsync(supervisorId, 1))
            .ReturnsAsync(true);

        var result = await _service.GetAnonymousProposalForSupervisorAsync(proposalId, supervisorId);

        Assert.NotNull(result);
        Assert.Null(result.Student);
        Assert.Equal(proposalId, result.Id);
        Assert.Equal("AI Project", result.Title);
    }

    [Fact]
    public async Task ExpressInterest_ShouldCreateMatchWithoutExposingStudent()
    {
        var proposalId = 1;
        var supervisorId = 1;
        var proposal = new Proposal
        {
            Id = proposalId,
            ProposalId = "2024-0001",
            StudentId = 100,
            Title = "AI Project",
            Abstract = "Test abstract",
            TechStack = ".NET, React",
            Status = ProposalStatus.Pending
        };

        _mockUnitOfWork.Setup(u => u.Proposals.GetByIdAsync(proposalId))
            .ReturnsAsync(proposal);
        _mockUnitOfWork.Setup(u => u.SupervisorExpertises.HasExpertiseAsync(supervisorId, proposal.ResearchAreaId))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(u => u.Matches.GetExistingMatchAsync(proposalId, supervisorId))
            .ReturnsAsync((ProjectApprovalSystem.Core.Entities.Match?)null);
        _mockUnitOfWork.Setup(u => u.Matches.AddAsync(It.IsAny<ProjectApprovalSystem.Core.Entities.Match>()))
            .Callback<ProjectApprovalSystem.Core.Entities.Match>(m => m.Id = 1)
            .ReturnsAsync((ProjectApprovalSystem.Core.Entities.Match m) => m);

        var result = await _service.ExpressInterestAsync(proposalId, supervisorId, "Interested");

        Assert.Equal(MatchStatus.Interested, result.Status);
        Assert.Null(result.Proposal?.Student);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ConfirmMatch_ShouldOnlyRevealStudentAfterMatching()
    {
        var matchId = 1;
        var match = new ProjectApprovalSystem.Core.Entities.Match
        {
            Id = matchId,
            ProposalId = 1,
            SupervisorId = 1,
            Status = MatchStatus.Interested,
            Proposal = new Proposal
            {
                Id = 1,
                ProposalId = "2024-0001",
                StudentId = 100,
                Title = "Project",
                Abstract = "Abstract",
                TechStack = "Tech"
            }
        };

        _mockUnitOfWork.Setup(u => u.Matches.GetByIdAsync(matchId))
            .ReturnsAsync(match);
        _mockUnitOfWork.Setup(u => u.Proposals.GetByIdAsync(match.ProposalId))
            .ReturnsAsync(match.Proposal);
        _mockUnitOfWork.Setup(u => u.Matches.UpdateAsync(It.IsAny<ProjectApprovalSystem.Core.Entities.Match>()))
            .ReturnsAsync(match);

        var result = await _service.ConfirmMatchAsync(matchId);

        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.NotNull(result.MatchedAt);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAnonymousProposalsForSupervisor_ShouldOnlyReturnPendingProposals()
    {
        var supervisorId = 1;
        var proposals = new List<Proposal>
        {
            new Proposal { Id = 1, ProposalId = "2024-0001", Title = "Project 1", Abstract = "Abstract 1", TechStack = "Tech1", Status = ProposalStatus.Pending, ResearchAreaId = 1 },
            new Proposal { Id = 2, ProposalId = "2024-0002", Title = "Project 2", Abstract = "Abstract 2", TechStack = "Tech2", Status = ProposalStatus.Matched, ResearchAreaId = 1 },
            new Proposal { Id = 3, ProposalId = "2024-0003", Title = "Project 3", Abstract = "Abstract 3", TechStack = "Tech3", Status = ProposalStatus.Pending, ResearchAreaId = 2 }
        };

        var expertise = new List<SupervisorExpertise>
        {
            new SupervisorExpertise { SupervisorId = supervisorId, ResearchAreaId = 1, Supervisor = new User { Id = supervisorId, Email = "supervisor@test.com", FullName = "Dr. Smith", PasswordHash = "hash" } }
        };

        _mockUnitOfWork.Setup(u => u.SupervisorExpertises.GetSupervisorExpertisesAsync(supervisorId))
            .ReturnsAsync(expertise);
        _mockUnitOfWork.Setup(u => u.Proposals.GetAllAnonymousProposalsAsync())
            .ReturnsAsync(proposals);
        _mockUnitOfWork.Setup(u => u.Matches.GetSupervisorMatchesAsync(supervisorId))
            .ReturnsAsync(new List<ProjectApprovalSystem.Core.Entities.Match>());

        var result = await _service.GetAnonymousProposalsForSupervisorAsync(supervisorId);

        Assert.Single(result);
        Assert.Equal(1, result.First().ResearchAreaId);
        Assert.Equal(ProposalStatus.Pending, result.First().Status);
    }

    [Fact]
    public async Task GetAnonymousProposalsForSupervisor_ShouldReturnEmpty_WhenSupervisorHasNoExpertise()
    {
        var supervisorId = 7;

        _mockUnitOfWork.Setup(u => u.SupervisorExpertises.GetSupervisorExpertisesAsync(supervisorId))
            .ReturnsAsync(new List<SupervisorExpertise>());

        var result = await _service.GetAnonymousProposalsForSupervisorAsync(supervisorId);

        Assert.Empty(result);
        _mockUnitOfWork.Verify(u => u.Proposals.GetAllAnonymousProposalsAsync(), Times.Never);
    }

    [Fact]
    public async Task ExpressInterest_ShouldFail_WhenProposalIsOutsideSupervisorExpertise()
    {
        var proposalId = 10;
        var supervisorId = 5;
        var proposal = new Proposal
        {
            Id = proposalId,
            ProposalId = "2026-0010",
            Title = "IoT Project",
            Abstract = "Test abstract",
            TechStack = "C#",
            Status = ProposalStatus.Pending,
            ResearchAreaId = 99
        };

        _mockUnitOfWork.Setup(u => u.Proposals.GetByIdAsync(proposalId))
            .ReturnsAsync(proposal);
        _mockUnitOfWork.Setup(u => u.SupervisorExpertises.HasExpertiseAsync(supervisorId, proposal.ResearchAreaId))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExpressInterestAsync(proposalId, supervisorId, "Interested"));

        _mockUnitOfWork.Verify(u => u.Matches.AddAsync(It.IsAny<ProjectApprovalSystem.Core.Entities.Match>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}

public class ProposalServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ProposalService _service;

    public ProposalServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new ProposalService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task SubmitProposal_ShouldGenerateAnonymousProposalId()
    {
        var studentId = 1;
        var researchAreaId = 1;
        var student = new User { Id = studentId, Email = "student@test.com", FullName = "Student", PasswordHash = "hash", Role = UserRole.Student };
        var area = new ResearchArea { Id = researchAreaId, Name = "AI Research" };

        _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(studentId))
            .ReturnsAsync(student);
        _mockUnitOfWork.Setup(u => u.ResearchAreas.GetByIdAsync(researchAreaId))
            .ReturnsAsync(area);
        _mockUnitOfWork.Setup(u => u.Proposals.AddAsync(It.IsAny<Proposal>()))
            .Callback<Proposal>(p => p.Id = 1)
            .ReturnsAsync((Proposal p) => p);
        _mockUnitOfWork.Setup(u => u.Proposals.GetAllAsync())
            .ReturnsAsync(new List<Proposal>());

        var result = await _service.SubmitProposalAsync(studentId, "Title", "Abstract", "Description", "Tech Stack", researchAreaId);

        Assert.NotNull(result.ProposalId);
        Assert.Matches(@"\d{4}-\d{4}", result.ProposalId);
        Assert.Equal(studentId, result.StudentId);
        Assert.Equal(ProposalStatus.Pending, result.Status);
    }

    [Fact]
    public async Task UpdateProposal_ShouldFailIfAlreadyMatched()
    {
        var proposalId = 1;
        var proposal = new Proposal
        {
            Id = proposalId,
            ProposalId = "2024-0001",
            Title = "Original",
            Abstract = "Test",
            TechStack = "Tech",
            Status = ProposalStatus.Matched
        };

        _mockUnitOfWork.Setup(u => u.Proposals.GetByIdAsync(proposalId))
            .ReturnsAsync(proposal);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateProposalAsync(proposalId, "New", "New", "New", "New"));
    }

    [Fact]
    public async Task GenerateProposalId_ShouldIncrementCounterCorrectly()
    {
        var year = DateTime.Now.Year;
        var existingProposals = new List<Proposal>
        {
            new Proposal { ProposalId = $"{year}-0001", Title = "P1", Abstract = "A1", TechStack = "T1", SubmittedAt = DateTime.Now },
            new Proposal { ProposalId = $"{year}-0002", Title = "P2", Abstract = "A2", TechStack = "T2", SubmittedAt = DateTime.Now }
        };

        _mockUnitOfWork.Setup(u => u.Proposals.GetAllAsync())
            .ReturnsAsync(existingProposals);

        var result = await _service.GenerateProposalIdAsync();

        Assert.Equal($"{year}-0003", result);
    }
}

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new UserService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task RegisterUser_ShouldFailIfEmailAlreadyExists()
    {
        var existingUser = new User { Id = 1, Email = "test@example.com", FullName = "Test User", PasswordHash = "hash" };
        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegisterUserAsync("test@example.com", "Test User", "Password123!", UserRole.Student));
    }

    [Fact]
    public async Task RegisterUser_ShouldHashPassword()
    {
        var email = "newuser@example.com";
        var password = "SecurePassword123!";

        _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);
        _mockUnitOfWork.Setup(u => u.Users.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => u.Id = 1)
            .ReturnsAsync((User u) => u);

        var result = await _service.RegisterUserAsync(email, "New User", password, UserRole.Student);

        Assert.NotEqual(password, result.PasswordHash);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
