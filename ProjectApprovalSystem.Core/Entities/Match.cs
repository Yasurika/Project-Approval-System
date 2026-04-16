namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Represents a supervisor's interest in or match with a proposal.
/// CRITICAL FOR BLIND MATCHING:
/// - Supervisor expresses interest in anonymous proposal
/// - Student identity is NOT revealed during this process
/// - Only after supervisor "Matches", student identity is revealed
/// </summary>
public class Match
{
    public int Id { get; set; }

    /// <summary>
    /// ID of the proposal (anonymous to supervisor before match)
    /// </summary>
    public int ProposalId { get; set; }

    /// <summary>
    /// Navigation: The proposal
    /// </summary>
    public virtual Proposal? Proposal { get; set; }

    /// <summary>
    /// ID of the supervisor
    /// </summary>
    public int SupervisorId { get; set; }

    /// <summary>
    /// Navigation: The supervisor
    /// </summary>
    public virtual User? Supervisor { get; set; }

    /// <summary>
    /// Status of the match/interest
    /// </summary>
    public MatchStatus Status { get; set; } = MatchStatus.Interested;

    /// <summary>
    /// Optional comments from supervisor about why they're interested
    /// </summary>
    public string? SupervisorComments { get; set; }

    /// <summary>
    /// When the supervisor expressed interest
    /// </summary>
    public DateTime InterestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the match was confirmed (if applicable)
    /// </summary>
    public DateTime? MatchedAt { get; set; }

    public override string ToString() => $"Match: {Supervisor?.FullName} - Proposal {Proposal?.ProposalId}";
}
