using ProjectApprovalSystem.Core.Entities;

namespace ProjectApprovalSystem.Core.Interfaces;

/// <summary>
/// Generic repository interface for basic CRUD operations
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}

/// <summary>
/// Repository for User entity operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetBySupervisorRoleAsync();
    Task<IEnumerable<User>> GetByStudentRoleAsync();
    Task<IEnumerable<User>> GetByCoordinatorRoleAsync();
}

/// <summary>
/// Repository for Proposal entity operations
/// CRITICAL: Includes methods that enforce blind matching
/// </summary>
public interface IProposalRepository : IRepository<Proposal>
{
    /// <summary>
    /// Get proposal without revealing student identity
    /// Used when supervisor is viewing proposals
    /// </summary>
    Task<Proposal?> GetAnonymousProposalAsync(int proposalId);

    /// <summary>
    /// Get all proposals without student details
    /// Used for displaying proposal list to supervisors
    /// </summary>
    Task<IEnumerable<Proposal>> GetAllAnonymousProposalsAsync();

    /// <summary>
    /// Get proposals filtered by research area (anonymously)
    /// </summary>
    Task<IEnumerable<Proposal>> GetProposalsByResearchAreaAsync(int researchAreaId);

    /// <summary>
    /// Get student's own proposals (with full details)
    /// </summary>
    Task<IEnumerable<Proposal>> GetStudentProposalsAsync(int studentId);

    /// <summary>
    /// Get proposals with pending status
    /// </summary>
    Task<IEnumerable<Proposal>> GetPendingProposalsAsync();
}

/// <summary>
/// Repository for Match entity operations
/// </summary>
public interface IMatchRepository : IRepository<Match>
{
    /// <summary>
    /// Get all matches for a supervisor
    /// </summary>
    Task<IEnumerable<Match>> GetSupervisorMatchesAsync(int supervisorId);

    /// <summary>
    /// Get all matches for a proposal
    /// </summary>
    Task<IEnumerable<Match>> GetProposalMatchesAsync(int proposalId);

    /// <summary>
    /// Check if supervisor already has interest/match with proposal
    /// </summary>
    Task<Match?> GetExistingMatchAsync(int proposalId, int supervisorId);

    /// <summary>
    /// Get confirmed matches (status = Matched)
    /// </summary>
    Task<IEnumerable<Match>> GetConfirmedMatchesAsync();
}

/// <summary>
/// Repository for ResearchArea entity operations
/// </summary>
public interface IResearchAreaRepository : IRepository<ResearchArea>
{
    Task<ResearchArea?> GetByNameAsync(string name);
    Task<IEnumerable<ResearchArea>> GetActiveAreasAsync();
}

/// <summary>
/// Repository for SupervisorExpertise entity operations
/// </summary>
public interface ISupervisorExpertiseRepository : IRepository<SupervisorExpertise>
{
    /// <summary>
    /// Get expertise areas for a supervisor
    /// </summary>
    Task<IEnumerable<SupervisorExpertise>> GetSupervisorExpertisesAsync(int supervisorId);

    /// <summary>
    /// Get supervisors with expertise in a research area
    /// </summary>
    Task<IEnumerable<SupervisorExpertise>> GetSupervisorsByResearchAreaAsync(int researchAreaId);

    /// <summary>
    /// Check if supervisor has expertise in area
    /// </summary>
    Task<bool> HasExpertiseAsync(int supervisorId, int researchAreaId);
}

/// <summary>
/// Unit of Work pattern interface for managing multiple repositories
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IProposalRepository Proposals { get; }
    IMatchRepository Matches { get; }
    IResearchAreaRepository ResearchAreas { get; }
    ISupervisorExpertiseRepository SupervisorExpertises { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
