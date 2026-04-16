using Microsoft.EntityFrameworkCore.Storage;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Data.Context;
using ProjectApprovalSystem.Data.Repositories;

namespace ProjectApprovalSystem.Data;

/// <summary>
/// Unit of Work pattern implementation
/// Manages transaction scope and coordinates multiple repositories
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly PasDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _userRepository;
    private IProposalRepository? _proposalRepository;
    private IMatchRepository? _matchRepository;
    private IResearchAreaRepository? _researchAreaRepository;
    private ISupervisorExpertiseRepository? _supervisorExpertiseRepository;

    public UnitOfWork(PasDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IProposalRepository Proposals => _proposalRepository ??= new ProposalRepository(_context);
    public IMatchRepository Matches => _matchRepository ??= new MatchRepository(_context);
    public IResearchAreaRepository ResearchAreas => _researchAreaRepository ??= new ResearchAreaRepository(_context);
    public ISupervisorExpertiseRepository SupervisorExpertises => _supervisorExpertiseRepository ??= new SupervisorExpertiseRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            await _transaction?.CommitAsync()!;
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            await _transaction?.RollbackAsync()!;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _transaction?.Dispose();
        await _context.DisposeAsync();
    }
}
