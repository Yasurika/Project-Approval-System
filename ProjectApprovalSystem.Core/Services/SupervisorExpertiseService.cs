using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;

namespace ProjectApprovalSystem.Core.Services;

/// <summary>
/// Service for supervisor expertise management
/// </summary>
public class SupervisorExpertiseService : ISupervisorExpertiseService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupervisorExpertiseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SupervisorExpertise> AssignExpertiseAsync(int supervisorId, int researchAreaId)
    {
        // Verify supervisor exists
        var supervisor = await _unitOfWork.Users.GetByIdAsync(supervisorId);
        if (supervisor == null || supervisor.Role != UserRole.Supervisor)
            throw new InvalidOperationException("Supervisor not found");

        // Verify research area exists
        var area = await _unitOfWork.ResearchAreas.GetByIdAsync(researchAreaId);
        if (area == null)
            throw new InvalidOperationException("Research area not found");

        // Check if expertise already exists
        var existing = await _unitOfWork.SupervisorExpertises
            .GetSupervisorsByResearchAreaAsync(researchAreaId);
        
        if (existing.Any(e => e.SupervisorId == supervisorId))
            throw new InvalidOperationException("Supervisor already has this expertise");

        var expertise = new SupervisorExpertise
        {
            SupervisorId = supervisorId,
            ResearchAreaId = researchAreaId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };

        await _unitOfWork.SupervisorExpertises.AddAsync(expertise);
        await _unitOfWork.SaveChangesAsync();

        return expertise;
    }

    public async Task<IEnumerable<SupervisorExpertise>> GetSupervisorExpertiseAsync(int supervisorId)
    {
        return await _unitOfWork.SupervisorExpertises.GetSupervisorExpertisesAsync(supervisorId);
    }

    public async Task RemoveExpertiseAsync(int supervisorId, int researchAreaId)
    {
        var expertise = await _unitOfWork.SupervisorExpertises
            .GetSupervisorsByResearchAreaAsync(researchAreaId);
        
        var toRemove = expertise.FirstOrDefault(e => e.SupervisorId == supervisorId);
        if (toRemove != null)
        {
            await _unitOfWork.SupervisorExpertises.DeleteAsync(toRemove.Id);
        }
    }
}
