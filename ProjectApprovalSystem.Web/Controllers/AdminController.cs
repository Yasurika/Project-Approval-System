using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Data.Context;
using ProjectApprovalSystem.Web.Infrastructure;
using ProjectApprovalSystem.Web.Models;

namespace ProjectApprovalSystem.Web.Controllers;

[RequireRole(UserRole.ModuleLeader, UserRole.Administrator)]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly IResearchAreaService _researchAreaService;
    private readonly PasDbContext _dbContext;

    public AdminController(
        IUserService userService,
        IResearchAreaService researchAreaService,
        PasDbContext dbContext)
    {
        _userService = userService;
        _researchAreaService = researchAreaService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var adminUser = await GetCurrentAdminAsync();

        var pendingProposals = await _dbContext.Proposals.CountAsync(proposal => proposal.Status == ProposalStatus.Pending);
        var matched = await _dbContext.Matches.CountAsync(match => match.Status == MatchStatus.Matched);
        var underReview = await _dbContext.Matches.CountAsync(match => match.Status == MatchStatus.Interested);

        var masterMatches = await _dbContext.Matches
            .Include(match => match.Supervisor)
            .Include(match => match.Proposal)
                .ThenInclude(proposal => proposal!.Student)
            .OrderByDescending(match => match.InterestedAt)
            .ToListAsync();

        var areas = (await _researchAreaService.GetAllAreasAsync()).OrderBy(area => area.Name).ToList();

        var users = await _dbContext.Users
            .Where(user => user.IsActive)
            .OrderBy(user => user.Role)
            .ThenBy(user => user.FullName)
            .ToListAsync();

        var supervisors = users
            .Where(user => user.Role == UserRole.Supervisor)
            .ToList();

        var activeStudents = users.Count(user => user.Role == UserRole.Student);
        var activeSupervisors = supervisors.Count;

        var vm = new AdminDashboardViewModel
        {
            AdminUser = adminUser,
            PendingProposalCount = pendingProposals,
            UnderReviewCount = underReview,
            MatchedCount = matched,
            ActiveStudentCount = activeStudents,
            ActiveSupervisorCount = activeSupervisors,
            MasterMatches = masterMatches,
            ResearchAreas = areas,
            Users = users,
            Supervisors = supervisors
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResearchArea(CreateResearchAreaAdminForm form)
    {
        _ = await GetCurrentAdminAsync();

        try
        {
            await _researchAreaService.CreateResearchAreaAsync(form.Name, form.Description);
            TempData["StatusMessage"] = "Research area added.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateResearchArea(UpdateResearchAreaAdminForm form)
    {
        _ = await GetCurrentAdminAsync();

        try
        {
            await _researchAreaService.UpdateResearchAreaAsync(form.AreaId, form.Name, form.Description);
            TempData["StatusMessage"] = "Research area updated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserAdminForm form)
    {
        _ = await GetCurrentAdminAsync();

        try
        {
            await _userService.RegisterUserAsync(form.Email, form.FullName, form.Password, form.Role);
            TempData["StatusMessage"] = "New user created.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceUnmatch(int matchId)
    {
        _ = await GetCurrentAdminAsync();

        var match = await _dbContext.Matches
            .Include(item => item.Proposal)
            .FirstOrDefaultAsync(item => item.Id == matchId);

        if (match == null)
        {
            TempData["ErrorMessage"] = "Match not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        match.Status = MatchStatus.Declined;
        match.MatchedAt = null;

        if (match.Proposal != null)
        {
            match.Proposal.Status = ProposalStatus.Pending;
        }

        await _dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Match removed and proposal moved back to Pending.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReassignMatch(ReassignMatchAdminForm form)
    {
        _ = await GetCurrentAdminAsync();

        var match = await _dbContext.Matches
            .Include(item => item.Proposal)
            .FirstOrDefaultAsync(item => item.Id == form.MatchId);

        if (match == null)
        {
            TempData["ErrorMessage"] = "Match not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        var newSupervisor = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == form.NewSupervisorId && user.Role == UserRole.Supervisor && user.IsActive);
        if (newSupervisor == null)
        {
            TempData["ErrorMessage"] = "Selected supervisor is invalid.";
            return RedirectToAction(nameof(Dashboard));
        }

        var duplicate = await _dbContext.Matches
            .FirstOrDefaultAsync(item => item.ProposalId == match.ProposalId && item.SupervisorId == form.NewSupervisorId && item.Id != match.Id);

        if (duplicate != null)
        {
            duplicate.Status = MatchStatus.Matched;
            duplicate.MatchedAt = DateTime.UtcNow;
            _dbContext.Matches.Remove(match);
        }
        else
        {
            match.SupervisorId = form.NewSupervisorId;
            match.Status = MatchStatus.Matched;
            match.MatchedAt = DateTime.UtcNow;
        }

        if (match.Proposal != null)
        {
            match.Proposal.Status = ProposalStatus.Matched;
        }

        await _dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Match reassigned successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    private async Task<User> GetCurrentAdminAsync()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        return (await _userService.GetUserAsync(userId))!;
    }
}
