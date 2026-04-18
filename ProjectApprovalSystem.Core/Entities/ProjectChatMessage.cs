namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Chat message inside a matched project's collaboration space.
/// </summary>
public class ProjectChatMessage
{
    public int Id { get; set; }

    public int ProposalId { get; set; }
    public virtual Proposal? Proposal { get; set; }

    public int SenderId { get; set; }
    public virtual User? Sender { get; set; }

    public required string MessageText { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
