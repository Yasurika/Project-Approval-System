namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Represents an additional student member assigned to a proposal team.
/// The proposal owner (StudentId on Proposal) is considered the group leader.
/// </summary>
public class ProposalGroupMember
{
    public int Id { get; set; }

    public int ProposalId { get; set; }
    public virtual Proposal? Proposal { get; set; }

    public int StudentId { get; set; }
    public virtual User? Student { get; set; }

    public GroupInviteStatus InviteStatus { get; set; } = GroupInviteStatus.Pending;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
}
