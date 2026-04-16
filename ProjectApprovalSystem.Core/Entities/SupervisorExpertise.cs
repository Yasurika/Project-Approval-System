namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Represents the areas of expertise/specialization of a supervisor.
/// A supervisor can have expertise in multiple research areas.
/// </summary>
public class SupervisorExpertise
{
    public int Id { get; set; }

    /// <summary>
    /// ID of the supervisor (User with role Supervisor)
    /// </summary>
    public int SupervisorId { get; set; }

    /// <summary>
    /// Navigation: The supervisor
    /// </summary>
    public virtual User? Supervisor { get; set; }

    /// <summary>
    /// ID of the research area
    /// </summary>
    public int ResearchAreaId { get; set; }

    /// <summary>
    /// Navigation: The research area
    /// </summary>
    public virtual ResearchArea? ResearchArea { get; set; }

    /// <summary>
    /// Indicates if this expertise assignment is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Assignment creation timestamp
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public override string ToString() => $"{Supervisor?.FullName} - {ResearchArea?.Name}";
}
