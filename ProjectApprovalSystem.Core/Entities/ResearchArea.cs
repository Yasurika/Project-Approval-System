namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Represents a research area or domain (e.g., AI, Web Development, Data Science)
/// Managed by Module Leaders
/// </summary>
public class ResearchArea
{
    public int Id { get; set; }

    /// <summary>
    /// Name of the research area (e.g., "Artificial Intelligence")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of the research area
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this research area is active for proposals
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Proposal> Proposals { get; set; } = [];
    public ICollection<SupervisorExpertise> SupervisorExpertises { get; set; } = [];

    public override string ToString() => Name;
}
