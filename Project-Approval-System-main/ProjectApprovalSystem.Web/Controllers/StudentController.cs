using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Core.Interfaces;
using ProjectApprovalSystem.Data.Context;
using ProjectApprovalSystem.Web.Infrastructure;
using ProjectApprovalSystem.Web.Models;

namespace ProjectApprovalSystem.Web.Controllers;

[RequireRole(UserRole.Student)]
public class StudentController : Controller
{
    private readonly IUserService _userService;
    private readonly IProposalService _proposalService;
    private readonly IResearchAreaService _researchAreaService;
    private readonly PasDbContext _dbContext;

    public StudentController(
        IUserService userService,
        IProposalService proposalService,
        IResearchAreaService researchAreaService,
        PasDbContext dbContext)
    {
        _userService = userService;
        _proposalService = proposalService;
        _researchAreaService = researchAreaService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var student = await GetCurrentStudentAsync();

        var proposals = (await _proposalService.GetStudentProposalsAsync(student.Id)).OrderByDescending(proposal => proposal.SubmittedAt).ToList();
        var areas = (await _researchAreaService.GetAllAreasAsync()).OrderBy(area => area.Name).ToList();

        var pendingInvites = await _dbContext.ProposalGroupMembers
            .Include(item => item.Proposal)
                .ThenInclude(proposal => proposal!.Student)
            .Where(item => item.StudentId == student.Id && item.InviteStatus == GroupInviteStatus.Pending)
            .OrderByDescending(item => item.AddedAt)
            .Select(item => new StudentGroupInviteViewModel
            {
                InvitationId = item.Id,
                ProposalId = item.ProposalId,
                ProposalCode = item.Proposal!.ProposalId,
                ProposalTitle = item.Proposal.Title,
                GroupLeaderName = item.Proposal.Student!.FullName,
                InvitedAt = item.AddedAt
            })
            .ToListAsync();

        var matches = await _dbContext.Matches
            .Include(match => match.Supervisor)
            .Include(match => match.Proposal)
            .Where(match => match.Proposal != null && match.Proposal.StudentId == student.Id)
            .ToListAsync();

        var matchedSupervisor = matches
            .Where(match => match.Status == MatchStatus.Matched)
            .Select(match => match.Supervisor)
            .FirstOrDefault(supervisor => supervisor != null);

        var hasInterested = matches.Any(match => match.Status == MatchStatus.Interested);
        var timelineStage = matchedSupervisor != null ? "Matched" : hasInterested ? "Under Review" : "Pending";

        var vm = new StudentDashboardViewModel
        {
            Student = student,
            Proposals = proposals,
            ResearchAreas = areas,
            PendingGroupInvites = pendingInvites,
            TimelineStage = timelineStage,
            MatchedSupervisor = matchedSupervisor
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitProposal(SubmitStudentProposalForm form)
    {
        var student = await GetCurrentStudentAsync();

        try
        {
            await _proposalService.SubmitProposalAsync(student.Id, form.Title, form.Abstract, form.Description, form.TechStack, form.ResearchAreaId);
            TempData["StatusMessage"] = "Proposal submitted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProposal(UpdateStudentProposalForm form)
    {
        var student = await GetCurrentStudentAsync();
        var proposal = await _proposalService.GetProposalAsync(form.ProposalId);
        if (proposal == null || proposal.StudentId != student.Id)
        {
            TempData["ErrorMessage"] = "Invalid proposal selection.";
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            await _proposalService.UpdateProposalAsync(form.ProposalId, form.Title, form.Abstract, form.Description, form.TechStack);
            TempData["StatusMessage"] = "Proposal updated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawProposal(int proposalId)
    {
        var student = await GetCurrentStudentAsync();

        var proposal = await _proposalService.GetProposalAsync(proposalId);
        if (proposal == null || proposal.StudentId != student.Id)
        {
            TempData["ErrorMessage"] = "Invalid proposal selection.";
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            await _proposalService.WithdrawProposalAsync(proposalId);
            TempData["StatusMessage"] = "Proposal withdrawn.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    private async Task<User> GetCurrentStudentAsync()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        return (await _userService.GetUserAsync(userId))!;
    }
}
