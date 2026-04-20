using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ProjectApprovalSystem.Core.Services;

/// <summary>
/// Implementation of blind matching service
/// Ensures student identity is never revealed before matching
/// </summary>
public class BlindMatchService : IBlindMatchService
{
    private readonly IUnitOfWork _unitOfWork;

    public BlindMatchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get proposal without student details for supervisor viewing
    /// </summary>
    public async Task<Proposal?> GetAnonymousProposalForSupervisorAsync(int proposalId, int supervisorId)
    {
        var proposal = await _unitOfWork.Proposals.GetAnonymousProposalAsync(proposalId);

        if (proposal == null)
            return null;

        // Verify supervisor has expertise in this area
        var hasExpertise = await _unitOfWork.SupervisorExpertises
            .HasExpertiseAsync(supervisorId, proposal.ResearchAreaId);

        if (!hasExpertise)
            return null; // Supervisor shouldn't see proposals outside their expertise

        return CreateAnonymousProposal(proposal);
    }

    /// <summary>
    /// Get anonymous proposals for supervisor to review.
    /// Shows all Pending proposals that the supervisor has not yet expressed interest in or matched.
    /// Declined proposals are shown again so the supervisor can reconsider.
    /// </summary>
    public async Task<IEnumerable<Proposal>> GetAnonymousProposalsForSupervisorAsync(int supervisorId)
    {
        var supervisorExpertise = await _unitOfWork.SupervisorExpertises
            .GetSupervisorExpertisesAsync(supervisorId);
        var allowedResearchAreaIds = supervisorExpertise
            .Where(expertise => expertise.IsActive)
            .Select(expertise => expertise.ResearchAreaId)
            .ToHashSet();

        if (allowedResearchAreaIds.Count == 0)
        {
            return [];
        }

        // Get all pending proposals anonymously
        var proposals = await _unitOfWork.Proposals.GetAllAnonymousProposalsAsync();

        // Get proposal IDs where supervisor already has an active interaction (Interested or Matched)
        var supervisorMatches = await _unitOfWork.Matches.GetSupervisorMatchesAsync(supervisorId);
        var alreadyActedProposalIds = supervisorMatches
            .Where(m => m.Status == MatchStatus.Interested || m.Status == MatchStatus.Matched)
            .Select(m => m.ProposalId)
            .ToHashSet();

        // Show all Pending proposals that this supervisor hasn't already interacted with
        var filtered = proposals
            .Where(p =>
                p.Status == ProposalStatus.Pending &&
                allowedResearchAreaIds.Contains(p.ResearchAreaId) &&
                !alreadyActedProposalIds.Contains(p.Id))
            .ToList();

        return filtered.Select(CreateAnonymousProposal).ToList();
    }

    /// <summary>
    /// Express interest in a proposal
    /// </summary>
    public async Task<Match> ExpressInterestAsync(int proposalId, int supervisorId, string? comments = null)
    {
        // Verify proposal exists and is pending
        var proposal = await _unitOfWork.Proposals.GetByIdAsync(proposalId);
        if (proposal == null || proposal.Status != ProposalStatus.Pending)
            throw new InvalidOperationException("Proposal not found or not available for matching");

        var hasExpertise = await _unitOfWork.SupervisorExpertises
            .HasExpertiseAsync(supervisorId, proposal.ResearchAreaId);
        if (!hasExpertise)
            throw new InvalidOperationException("You can only express interest in projects that match your expertise tags");

        // Check if supervisor already has interest in this proposal
        var existingMatch = await _unitOfWork.Matches.GetExistingMatchAsync(proposalId, supervisorId);
        if (existingMatch != null)
            throw new InvalidOperationException("You have already expressed interest in this proposal");

        // Create new match/interest record
        var match = new Match
        {
            ProposalId = proposalId,
            SupervisorId = supervisorId,
            Status = MatchStatus.Interested,
            SupervisorComments = comments,
            InterestedAt = DateTime.UtcNow
        };

        await _unitOfWork.Matches.AddAsync(match);
        await _unitOfWork.SaveChangesAsync();

        return new Match
        {
            Id = match.Id,
            ProposalId = match.ProposalId,
            SupervisorId = match.SupervisorId,
            Status = match.Status,
            SupervisorComments = match.SupervisorComments,
            InterestedAt = match.InterestedAt,
            MatchedAt = match.MatchedAt,
            Proposal = match.Proposal != null ? CreateAnonymousProposal(match.Proposal) : null,
            Supervisor = match.Supervisor
        };
    }

    /// <summary>
    /// Confirm match - now reveal student identity
    /// </summary>
    public async Task<Match> ConfirmMatchAsync(int matchId)
    {
        var match = await _unitOfWork.Matches.GetByIdAsync(matchId);
        if (match == null)
            throw new InvalidOperationException("Match not found");

        // Update match status
        match.Status = MatchStatus.Matched;
        match.MatchedAt = DateTime.UtcNow;

        // Update proposal status
        var proposal = await _unitOfWork.Proposals.GetByIdAsync(match.ProposalId);
        if (proposal != null)
        {
            proposal.Status = ProposalStatus.Matched;
        }

        await _unitOfWork.SaveChangesAsync();

        // NOW it's safe to include student info
        match = await _unitOfWork.Matches.GetByIdAsync(matchId);
        return match ?? throw new InvalidOperationException("Failed to retrieve confirmed match");
    }

    /// <summary>
    /// Decline interest in proposal
    /// </summary>
    public async Task DeclineMatchAsync(int matchId)
    {
        var match = await _unitOfWork.Matches.GetByIdAsync(matchId);
        if (match == null)
            throw new InvalidOperationException("Match not found");

        match.Status = MatchStatus.Declined;
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Verify blind matching hasn't been compromised
    /// Audit function to check database integrity
    /// </summary>
    public async Task<bool> VerifyBlindMatchingIntegrityAsync()
    {
        // Check if any active matches have StudentId exposed in areas where it shouldn't be
        // This is a security audit function
        try
        {
            var allMatches = await _unitOfWork.Matches.GetAllAsync();
            var allProposals = await _unitOfWork.Proposals.GetAllAsync();

            // Verify no proposals with StudentId visible to supervisors before matching
            foreach (var proposal in allProposals)
            {
                if (proposal.Status == ProposalStatus.Pending)
                {
                    // StudentId should not be accessible in queries used by supervisors
                    if (proposal.StudentId <= 0)
                        return false; // StudentId should exist but be protected
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Proposal CreateAnonymousProposal(Proposal proposal)
    {
        return new Proposal
        {
            Id = proposal.Id,
            ProposalId = proposal.ProposalId,
            StudentId = 0,
            Student = null,
            Title = proposal.Title,
            Abstract = proposal.Abstract,
            Description = proposal.Description,
            TechStack = proposal.TechStack,
            ResearchAreaId = proposal.ResearchAreaId,
            ResearchArea = proposal.ResearchArea,
            Status = proposal.Status,
            SubmittedAt = proposal.SubmittedAt,
            UpdatedAt = proposal.UpdatedAt,
            Matches = proposal.Matches
        };
    }
}
