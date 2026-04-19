using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;

namespace ProjectApprovalSystem.Core.Services;

/// <summary>
/// Service for proposal management
/// </summary>
public class ProposalService : IProposalService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProposalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Proposal> SubmitProposalAsync(int studentId, string title, string @abstract, 
        string description, string techStack, int researchAreaId)
    {
        // Verify student exists
        var student = await _unitOfWork.Users.GetByIdAsync(studentId);
        if (student == null || student.Role != UserRole.Student)
            throw new InvalidOperationException("Student not found");

        // Verify research area exists
        var researchArea = await _unitOfWork.ResearchAreas.GetByIdAsync(researchAreaId);
        if (researchArea == null)
            throw new InvalidOperationException("Research area not found");

        // Generate anonymous proposal ID
        var proposalId = await GenerateProposalIdAsync();

        var proposal = new Proposal
        {
            ProposalId = proposalId,
            StudentId = studentId,
            Title = title,
            Abstract = @abstract,
            Description = description,
            TechStack = techStack,
            ResearchAreaId = researchAreaId,
            Status = ProposalStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        await _unitOfWork.Proposals.AddAsync(proposal);
        await _unitOfWork.SaveChangesAsync();

        return proposal;
    }

    public async Task<Proposal?> GetProposalAsync(int proposalId)
    {
        return await _unitOfWork.Proposals.GetByIdAsync(proposalId);
    }

    public async Task<IEnumerable<Proposal>> GetStudentProposalsAsync(int studentId)
    {
        return await _unitOfWork.Proposals.GetStudentProposalsAsync(studentId);
    }

    public async Task<Proposal> UpdateProposalAsync(int proposalId, string title, string @abstract, 
        string description, string techStack)
    {
        var proposal = await _unitOfWork.Proposals.GetByIdAsync(proposalId);
        if (proposal == null)
            throw new InvalidOperationException("Proposal not found");

        if (proposal.Status == ProposalStatus.Matched)
            throw new InvalidOperationException("Cannot modify matched proposal");

        proposal.Title = title;
        proposal.Abstract = @abstract;
        proposal.Description = description;
        proposal.TechStack = techStack;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Proposals.UpdateAsync(proposal);
        await _unitOfWork.SaveChangesAsync();

        return proposal;
    }

    public async Task WithdrawProposalAsync(int proposalId)
    {
        var proposal = await _unitOfWork.Proposals.GetByIdAsync(proposalId);
        if (proposal == null)
            throw new InvalidOperationException("Proposal not found");

        if (proposal.Status == ProposalStatus.Matched)
            throw new InvalidOperationException("Cannot withdraw matched proposal");

        proposal.Status = ProposalStatus.Withdrawn;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Proposals.UpdateAsync(proposal);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<string> GenerateProposalIdAsync()
    {
        var year = DateTime.Now.Year;
        var allProposals = await _unitOfWork.Proposals.GetAllAsync();
        
        // Get count of proposals for this year
        var count = allProposals
            .Where(p => p.SubmittedAt.Year == year)
            .Count() + 1;

        return $"{year}-{count:D4}";
    }
}
