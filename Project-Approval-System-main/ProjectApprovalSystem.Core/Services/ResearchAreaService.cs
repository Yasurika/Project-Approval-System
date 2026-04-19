using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;

namespace ProjectApprovalSystem.Core.Services;

/// <summary>
/// Service for research area management
/// </summary>
public class ResearchAreaService : IResearchAreaService
{
    private readonly IUnitOfWork _unitOfWork;

    public ResearchAreaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResearchArea> CreateResearchAreaAsync(string name, string? description)
    {
        var existingArea = await _unitOfWork.ResearchAreas.GetByNameAsync(name);
        if (existingArea != null)
            throw new InvalidOperationException("Research area already exists");

        var area = new ResearchArea
        {
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ResearchAreas.AddAsync(area);
        await _unitOfWork.SaveChangesAsync();

        return area;
    }

    public async Task<IEnumerable<ResearchArea>> GetAllAreasAsync()
    {
        return await _unitOfWork.ResearchAreas.GetActiveAreasAsync();
    }

    public async Task<ResearchArea> UpdateResearchAreaAsync(int areaId, string name, string? description)
    {
        var area = await _unitOfWork.ResearchAreas.GetByIdAsync(areaId);
        if (area == null)
            throw new InvalidOperationException("Research area not found");

        area.Name = name;
        area.Description = description;

        await _unitOfWork.ResearchAreas.UpdateAsync(area);
        await _unitOfWork.SaveChangesAsync();

        return area;
    }
}
