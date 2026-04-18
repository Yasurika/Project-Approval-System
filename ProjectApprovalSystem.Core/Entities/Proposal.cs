namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Represents a project proposal submitted by a student.
/// CRITICAL: Student identity is NOT visible to supervisors until after matching.
/// Supervisors see only ProposalId, Title, Abstract, and TechStack.
/// </summary>
public class Proposal
{
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier shown to supervisors (instead of student name)
    /// Format: YYYY-XXXX (e.g., 2024-0001)
    /// </summary>
    public required string ProposalId { get; set; }

    /// <summary>
    /// ID of the student who submitted this proposal
    /// HIDDEN from supervisors until after matching
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>
    /// Navigation: Student who submitted this proposal
    /// HIDDEN from supervisors until after matching
    /// </summary>
    public virtual User? Student { get; set; }

    /// <summary>
    /// Title of the project proposal
    /// VISIBLE to supervisors
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Abstract/summary of the project
    /// VISIBLE to supervisors
    /// </summary>
    public required string Abstract { get; set; }

    /// <summary>
    /// Detailed description of the project
    /// VISIBLE to supervisors
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Technology stack required (e.g., ".NET, React, SQL Server")
    /// VISIBLE to supervisors
    /// </summary>
    public required string TechStack { get; set; }

    /// <summary>
    /// Research area/domain
    /// VISIBLE to supervisors (helps them filter)
    /// </summary>
    public int ResearchAreaId { get; set; }

    /// <summary>
    /// Navigation: Research area this proposal belongs to
    /// </summary>
    public virtual ResearchArea? ResearchArea { get; set; }

    /// <summary>
    /// Current status of the proposal
    /// </summary>
    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    /// <summary>
    /// Submission timestamp
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Match> Matches { get; set; } = [];
    public ICollection<ProposalGroupMember> GroupMembers { get; set; } = [];
    public ICollection<ProjectChatMessage> ChatMessages { get; set; } = [];

    public override string ToString() => $"{ProposalId} - {Title}";
}
