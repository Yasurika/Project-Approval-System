using ProjectApprovalSystem.Core.Entities;

namespace ProjectApprovalSystem.Core.Interfaces;

/// <summary>
/// Service for handling blind matching logic
/// CRITICAL: Ensures student identity is never revealed before matching
/// </summary>
public interface IBlindMatchService
{
    /// <summary>
    /// Get anonymous proposal (no student details) for supervisor viewing
    /// </summary>
    Task<Proposal?> GetAnonymousProposalForSupervisorAsync(int proposalId, int supervisorId);

    /// <summary>
    /// Get all anonymous proposals(filtered by supervisor's expertise)
    /// </summary>
    Task<IEnumerable<Proposal>> GetAnonymousProposalsForSupervisorAsync(int supervisorId);

    /// <summary>
    /// Supervisor expresses interest in an anonymous proposal
    /// </summary>
    Task<Match> ExpressInterestAsync(int proposalId, int supervisorId, string? comments = null);

    /// <summary>
    /// Confirm match between supervisor and student
    /// After this, student identity is revealed to supervisor
    /// </summary>
    Task<Match> ConfirmMatchAsync(int matchId);

    /// <summary>
    /// Decline interest in a proposal
    /// </summary>
    Task DeclineMatchAsync(int matchId);

    /// <summary>
    /// Verify that blind matching hasn't been compromised
    /// Used for security audit
    /// </summary>
    Task<bool> VerifyBlindMatchingIntegrityAsync();
}

/// <summary>
/// Service for user management and authentication
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Register new user
    /// </summary>
    Task<User> RegisterUserAsync(string email, string fullName, string password, UserRole role);

    /// <summary>
    /// Authenticate user
    /// </summary>
    Task<User?> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<User?> GetUserAsync(int id);

    /// <summary>
    /// Get all supervisors
    /// </summary>
    Task<IEnumerable<User>> GetAllSupervisorsAsync();

    /// <summary>
    /// Deactivate user account
    /// </summary>
    Task DeactivateUserAsync(int userId);

    /// <summary>
    /// Verify password
    /// </summary>
    Task<bool> VerifyPasswordAsync(User user, string password);
}

/// <summary>
/// Service for proposal management
/// </summary>
public interface IProposalService
{
    /// <summary>
    /// Submit new proposal anonymously
    /// </summary>
    Task<Proposal> SubmitProposalAsync(int studentId, string title, string @abstract, string description, 
        string techStack, int researchAreaId);

    /// <summary>
    /// Get proposal by student (visible to owner)
    /// </summary>
    Task<Proposal?> GetProposalAsync(int proposalId);

    /// <summary>
    /// Get all proposals by student
    /// </summary>
    Task<IEnumerable<Proposal>> GetStudentProposalsAsync(int studentId);

    /// <summary>
    /// Update proposal (only if not matched)
    /// </summary>
    Task<Proposal> UpdateProposalAsync(int proposalId, string title, string @abstract, string description, string techStack);

    /// <summary>
    /// Withdraw proposal
    /// </summary>
    Task WithdrawProposalAsync(int proposalId);

    /// <summary>
    /// Generate unique anonymous proposal ID
    /// </summary>
    Task<string> GenerateProposalIdAsync();
}

/// <summary>
/// Service for research area management
/// </summary>
public interface IResearchAreaService
{
    /// <summary>
    /// Create research area (admin/coordinator only)
    /// </summary>
    Task<ResearchArea> CreateResearchAreaAsync(string name, string? description);

    /// <summary>
    /// Get all active research areas
    /// </summary>
    Task<IEnumerable<ResearchArea>> GetAllAreasAsync();

    /// <summary>
    /// Update research area
    /// </summary>
    Task<ResearchArea> UpdateResearchAreaAsync(int areaId, string name, string? description);
}

/// <summary>
/// Service for supervisor expertise management
/// </summary>
public interface ISupervisorExpertiseService
{
    /// <summary>
    /// Assign expertise area to supervisor
    /// </summary>
    Task<SupervisorExpertise> AssignExpertiseAsync(int supervisorId, int researchAreaId);

    /// <summary>
    /// Get supervisor expertise areas
    /// </summary>
    Task<IEnumerable<SupervisorExpertise>> GetSupervisorExpertiseAsync(int supervisorId);

    /// <summary>
    /// Remove expertise from supervisor
    /// </summary>
    Task RemoveExpertiseAsync(int supervisorId, int researchAreaId);
}
