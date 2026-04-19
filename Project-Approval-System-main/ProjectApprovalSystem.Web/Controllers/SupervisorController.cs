using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Data.Context;
using ProjectApprovalSystem.Web.Infrastructure;
using ProjectApprovalSystem.Web.Models;

namespace ProjectApprovalSystem.Web.Controllers;

[RequireRole(UserRole.Supervisor)]
public class SupervisorController : Controller
{
    private readonly IUserService _userService;
    private readonly IBlindMatchService _blindMatchService;
    private readonly ISupervisorExpertiseService _supervisorExpertiseService;
    private readonly IResearchAreaService _researchAreaService;
    private readonly PasDbContext _dbContext;

    public SupervisorController(
        IUserService userService,
        IBlindMatchService blindMatchService,
        ISupervisorExpertiseService supervisorExpertiseService,
        IResearchAreaService researchAreaService,
        PasDbContext dbContext)
    {
        _userService = userService;
        _blindMatchService = blindMatchService;
        _supervisorExpertiseService = supervisorExpertiseService;
        _researchAreaService = researchAreaService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(int? revealMatchId = null)
    {
        var supervisor = await GetCurrentSupervisorAsync();

        var areas = (await _researchAreaService.GetAllAreasAsync()).OrderBy(area => area.Name).ToList();
        var expertise = (await _supervisorExpertiseService.GetSupervisorExpertiseAsync(supervisor.Id)).ToList();
        var selectedIds = expertise.Select(item => item.ResearchAreaId).ToHashSet();

        var anonymousProposals = (await _blindMatchService.GetAnonymousProposalsForSupervisorAsync(supervisor.Id)).ToList();

        var pendingInterests = await _dbContext.Matches
            .Include(match => match.Proposal)
            .Where(match => match.SupervisorId == supervisor.Id && match.Status == MatchStatus.Interested)
            .OrderByDescending(match => match.InterestedAt)
            .ToListAsync();

        var confirmedMatches = await _dbContext.Matches
            .Include(match => match.Proposal)
                .ThenInclude(proposal => proposal!.Student)
            .Where(match => match.SupervisorId == supervisor.Id && match.Status == MatchStatus.Matched)
            .OrderByDescending(match => match.MatchedAt)
            .ToListAsync();

        Match? revealedMatch = null;
        if (revealMatchId.HasValue)
        {
            revealedMatch = confirmedMatches.FirstOrDefault(match => match.Id == revealMatchId.Value);
        }

        var vm = new SupervisorDashboardViewModel
        {
            Supervisor = supervisor,
            ResearchAreas = areas,
            SelectedResearchAreaIds = selectedIds,
            AnonymousProposals = anonymousProposals,
            PendingInterests = pendingInterests,
            ConfirmedMatches = confirmedMatches,
            RevealedMatch = revealedMatch
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveExpertise(SaveExpertiseForm form)
    {
        var supervisor = await GetCurrentSupervisorAsync();

        var current = (await _supervisorExpertiseService.GetSupervisorExpertiseAsync(supervisor.Id)).ToList();
        var currentIds = current.Select(item => item.ResearchAreaId).ToHashSet();
        var newIds = form.ResearchAreaIds.ToHashSet();

        foreach (var removeId in currentIds.Except(newIds))
        {
            await _supervisorExpertiseService.RemoveExpertiseAsync(supervisor.Id, removeId);
        }

        foreach (var addId in newIds.Except(currentIds))
        {
            try
            {
                await _supervisorExpertiseService.AssignExpertiseAsync(supervisor.Id, addId);
            }
            catch (InvalidOperationException)
            {
                // Ignore duplicates from race conditions.
            }
        }

        TempData["StatusMessage"] = "Expertise tags updated.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressInterest(ConfirmMatchFromFeedForm form)
    {
        var supervisor = await GetCurrentSupervisorAsync();

        try
        {
            var existing = await _dbContext.Matches.FirstOrDefaultAsync(match =>
                match.ProposalId == form.ProposalId && match.SupervisorId == supervisor.Id);

            if (existing != null)
            {
                TempData["StatusMessage"] = "You have already expressed interest in this proposal.";
                return RedirectToAction(nameof(Dashboard));
            }

            await _blindMatchService.ExpressInterestAsync(form.ProposalId, supervisor.Id, form.Comments);
            TempData["StatusMessage"] = "Interest expressed. The proposal has moved to your Pending Interests.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmMatch(int matchId)
    {
        var supervisor = await GetCurrentSupervisorAsync();

        try
        {
            var match = await _dbContext.Matches.FirstOrDefaultAsync(m =>
                m.Id == matchId && m.SupervisorId == supervisor.Id);

            if (match == null)
            {
                TempData["ErrorMessage"] = "Match record not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            var confirmed = await _blindMatchService.ConfirmMatchAsync(matchId);
            TempData["StatusMessage"] = "Match confirmed. Student identity has been revealed.";
            return RedirectToAction(nameof(Dashboard), new { revealMatchId = confirmed.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmFromFeed(ConfirmMatchFromFeedForm form)
    {
        // Legacy alias - redirects to ExpressInterest
        return await ExpressInterest(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineInterest(int matchId)
    {
        var supervisor = await GetCurrentSupervisorAsync();

        try
        {
            await _blindMatchService.DeclineMatchAsync(matchId);
            TempData["StatusMessage"] = "Interest declined.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    private async Task<User> GetCurrentSupervisorAsync()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        return (await _userService.GetUserAsync(userId))!;
    }
}
