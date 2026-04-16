using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Data.Context;

namespace ProjectApprovalSystem.Data.Repositories;

/// <summary>
/// Generic repository base class
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly PasDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(PasDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        var entry = _context.Entry(entity);

        if (entry.State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
            entry = _context.Entry(entity);
        }

        entry.State = EntityState.Modified;
        return await Task.FromResult(entity);
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Repository for User entity
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(PasDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetBySupervisorRoleAsync()
    {
        return await _dbSet
            .Where(u => u.Role == UserRole.Supervisor && u.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByStudentRoleAsync()
    {
        return await _dbSet
            .Where(u => u.Role == UserRole.Student && u.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByCoordinatorRoleAsync()
    {
        return await _dbSet
            .Where(u => u.Role == UserRole.ModuleLeader && u.IsActive)
            .ToListAsync();
    }
}

/// <summary>
/// Repository for Proposal entity
/// CRITICAL: Implements blind matching by controlling data visibility
/// </summary>
public class ProposalRepository : Repository<Proposal>, IProposalRepository
{
    public ProposalRepository(PasDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get proposal without revealing student identity
    /// </summary>
    public async Task<Proposal?> GetAnonymousProposalAsync(int proposalId)
    {
        var proposal = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proposalId);

        if (proposal != null)
        {
            // CRITICAL: Don't load student navigation property
            proposal.Student = null;
        }

        return proposal;
    }

    /// <summary>
    /// Get all proposals without student details
    /// </summary>
    public async Task<IEnumerable<Proposal>> GetAllAnonymousProposalsAsync()
    {
        var proposals = await _dbSet
            .AsNoTracking()
            .Select(p => new Proposal
            {
                Id = p.Id,
                ProposalId = p.ProposalId,
                Title = p.Title,
                Abstract = p.Abstract,
                Description = p.Description,
                TechStack = p.TechStack,
                ResearchAreaId = p.ResearchAreaId,
                Status = p.Status,
                SubmittedAt = p.SubmittedAt,
                UpdatedAt = p.UpdatedAt,
                // CRITICAL: StudentId and Student not included
                StudentId = 0 // Placeholder, not from DB
            })
            .ToListAsync();

        return proposals;
    }

    public async Task<IEnumerable<Proposal>> GetProposalsByResearchAreaAsync(int researchAreaId)
    {
        return await _dbSet
            .Where(p => p.ResearchAreaId == researchAreaId && p.Status == ProposalStatus.Pending)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Proposal>> GetStudentProposalsAsync(int studentId)
    {
        return await _dbSet
            .Where(p => p.StudentId == studentId)
            .Include(p => p.ResearchArea)
            .ToListAsync();
    }

    public async Task<IEnumerable<Proposal>> GetPendingProposalsAsync()
    {
        return await _dbSet
            .Where(p => p.Status == ProposalStatus.Pending)
            .ToListAsync();
    }
}

/// <summary>
/// Repository for Match entity
/// </summary>
public class MatchRepository : Repository<Match>, IMatchRepository
{
    public MatchRepository(PasDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Match>> GetSupervisorMatchesAsync(int supervisorId)
    {
        return await _dbSet
            .Where(m => m.SupervisorId == supervisorId)
            .Include(m => m.Proposal)
            .Include(m => m.Supervisor)
            .ToListAsync();
    }

    public async Task<IEnumerable<Match>> GetProposalMatchesAsync(int proposalId)
    {
        return await _dbSet
            .Where(m => m.ProposalId == proposalId)
            .Include(m => m.Supervisor)
            .ToListAsync();
    }

    public async Task<Match?> GetExistingMatchAsync(int proposalId, int supervisorId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.ProposalId == proposalId && m.SupervisorId == supervisorId);
    }

    public async Task<IEnumerable<Match>> GetConfirmedMatchesAsync()
    {
        return await _dbSet
            .Where(m => m.Status == MatchStatus.Matched)
            .Include(m => m.Proposal)
            .Include(m => m.Supervisor)
            .ToListAsync();
    }
}

/// <summary>
/// Repository for ResearchArea entity
/// </summary>
public class ResearchAreaRepository : Repository<ResearchArea>, IResearchAreaRepository
{
    public ResearchAreaRepository(PasDbContext context) : base(context)
    {
    }

    public async Task<ResearchArea?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<IEnumerable<ResearchArea>> GetActiveAreasAsync()
    {
        return await _dbSet
            .Where(r => r.IsActive)
            .ToListAsync();
    }
}

/// <summary>
/// Repository for SupervisorExpertise entity
/// </summary>
public class SupervisorExpertiseRepository : Repository<SupervisorExpertise>, ISupervisorExpertiseRepository
{
    public SupervisorExpertiseRepository(PasDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SupervisorExpertise>> GetSupervisorExpertisesAsync(int supervisorId)
    {
        return await _dbSet
            .Where(se => se.SupervisorId == supervisorId && se.IsActive)
            .Include(se => se.ResearchArea)
            .ToListAsync();
    }

    public async Task<IEnumerable<SupervisorExpertise>> GetSupervisorsByResearchAreaAsync(int researchAreaId)
    {
        return await _dbSet
            .Where(se => se.ResearchAreaId == researchAreaId && se.IsActive)
            .Include(se => se.Supervisor)
            .ToListAsync();
    }

    public async Task<bool> HasExpertiseAsync(int supervisorId, int researchAreaId)
    {
        return await _dbSet
            .AnyAsync(se => se.SupervisorId == supervisorId && se.ResearchAreaId == researchAreaId && se.IsActive);
    }
}
