namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Represents a user in the system (Student, Supervisor, Module Leader, or Administrator)
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Unique email address for login
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Full name of the user
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Hashed password
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Role type: Student, Supervisor, ModuleLeader, or Administrator
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether the account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last account update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Proposal> StudentProposals { get; set; } = [];
    public ICollection<SupervisorExpertise> Expertises { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];

    public override string ToString() => $"{FullName} ({Role})";
}
