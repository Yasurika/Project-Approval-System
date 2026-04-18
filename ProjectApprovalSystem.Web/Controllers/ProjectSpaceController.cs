using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectApprovalSystem.Core.Entities;
using ProjectApprovalSystem.Data.Context;
using ProjectApprovalSystem.Web.Infrastructure;
using ProjectApprovalSystem.Web.Models;

namespace ProjectApprovalSystem.Web.Controllers;

[RequireRole(UserRole.Student, UserRole.Supervisor)]
public class ProjectSpaceController : Controller
{
    private readonly PasDbContext _dbContext;

    public ProjectSpaceController(PasDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUserAsync();

        var items = new List<ProjectSpaceListItemViewModel>();
        var pendingInvitations = new List<StudentGroupInviteViewModel>();

        if (currentUser.Role == UserRole.Student)
        {
            var leaderProposals = await _dbContext.Proposals
                .Where(item => item.StudentId == currentUser.Id)
                .OrderByDescending(item => item.SubmittedAt)
                .ToListAsync();

            items.AddRange(leaderProposals.Select(proposal => new ProjectSpaceListItemViewModel
            {
                ProposalId = proposal.Id,
                ProposalCode = proposal.ProposalId,
                Title = proposal.Title,
                RoleInProject = "Group Leader",
                ProposalStatus = proposal.Status
            }));

            var acceptedMemberships = await _dbContext.ProposalGroupMembers
                .Include(item => item.Proposal)
                .Where(item => item.StudentId == currentUser.Id && item.InviteStatus == GroupInviteStatus.Accepted)
                .OrderByDescending(item => item.AcceptedAt)
                .ToListAsync();

            items.AddRange(acceptedMemberships
                .Where(item => item.Proposal != null)
                .Select(item => new ProjectSpaceListItemViewModel
                {
                    ProposalId = item.Proposal!.Id,
                    ProposalCode = item.Proposal.ProposalId,
                    Title = item.Proposal.Title,
                    RoleInProject = "Group Member",
                    ProposalStatus = item.Proposal.Status
                }));

            pendingInvitations = await _dbContext.ProposalGroupMembers
                .Include(item => item.Proposal)
                    .ThenInclude(proposal => proposal!.Student)
                .Where(item => item.StudentId == currentUser.Id && item.InviteStatus == GroupInviteStatus.Pending)
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
        }
        else if (currentUser.Role == UserRole.Supervisor)
        {
            var matchedProposals = await _dbContext.Matches
                .Include(item => item.Proposal)
                .Where(item => item.SupervisorId == currentUser.Id && item.Status == MatchStatus.Matched && item.Proposal != null)
                .OrderByDescending(item => item.MatchedAt)
                .Select(item => item.Proposal!)
                .ToListAsync();

            items.AddRange(matchedProposals.Select(proposal => new ProjectSpaceListItemViewModel
            {
                ProposalId = proposal.Id,
                ProposalCode = proposal.ProposalId,
                Title = proposal.Title,
                RoleInProject = "Matched Supervisor",
                ProposalStatus = proposal.Status
            }));
        }

        var vm = new ProjectSpaceIndexViewModel
        {
            CurrentUser = currentUser,
            AccessibleProjects = items
                .GroupBy(item => item.ProposalId)
                .Select(group => group.First())
                .OrderByDescending(item => item.ProposalId)
                .ToList(),
            PendingInvitations = pendingInvitations
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int proposalId)
    {
        var currentUser = await GetCurrentUserAsync();

        var proposal = await _dbContext.Proposals
            .Include(item => item.Student)
            .Include(item => item.ResearchArea)
            .FirstOrDefaultAsync(item => item.Id == proposalId);

        if (proposal == null)
        {
            TempData["ErrorMessage"] = "Project not found.";
            return RedirectToRoleDashboard(currentUser.Role);
        }

        var groupMembers = await _dbContext.ProposalGroupMembers
            .Include(item => item.Student)
            .Where(item => item.ProposalId == proposalId)
            .OrderBy(item => item.Student!.FullName)
            .ToListAsync();

        var acceptedMembers = groupMembers.Where(member => member.InviteStatus == GroupInviteStatus.Accepted).ToList();
        var pendingMembers = groupMembers.Where(member => member.InviteStatus == GroupInviteStatus.Pending).ToList();

        var matchedRecord = await _dbContext.Matches
            .Include(item => item.Supervisor)
            .Where(item => item.ProposalId == proposalId && item.Status == MatchStatus.Matched)
            .OrderByDescending(item => item.MatchedAt)
            .FirstOrDefaultAsync();

        var matchedSupervisor = matchedRecord?.Supervisor;

        var isGroupLeader = currentUser.Id == proposal.StudentId;
        var isGroupMember = acceptedMembers.Any(member => member.StudentId == currentUser.Id);
        var isMatchedSupervisor = matchedSupervisor?.Id == currentUser.Id;

        var canAccess = isGroupLeader || isGroupMember || isMatchedSupervisor;
        if (!canAccess)
        {
            TempData["ErrorMessage"] = "You are not authorized to access this project space.";
            return RedirectToRoleDashboard(currentUser.Role);
        }

        var availableStudents = new List<User>();
        if (isGroupLeader)
        {
            var existingIds = groupMembers.Select(member => member.StudentId).ToHashSet();
            existingIds.Add(proposal.StudentId);

            availableStudents = await _dbContext.Users
                .Where(user => user.IsActive && user.Role == UserRole.Student && !existingIds.Contains(user.Id))
                .OrderBy(user => user.FullName)
                .ToListAsync();
        }

        var isChatEnabled = matchedRecord != null;

        var chatMessages = new List<ProjectChatMessage>();
        if (isChatEnabled)
        {
            chatMessages = await _dbContext.ProjectChatMessages
                .Include(item => item.Sender)
                .Where(item => item.ProposalId == proposalId)
                .OrderBy(item => item.SentAt)
                .ToListAsync();
        }

        var progressStage = proposal.Status == ProposalStatus.Matched
            ? "Matched"
            : await _dbContext.Matches.AnyAsync(item => item.ProposalId == proposalId && item.Status == MatchStatus.Interested)
                ? "Under Review"
                : "Pending";

        var vm = new ProjectSpaceViewModel
        {
            Proposal = proposal,
            CurrentUser = currentUser,
            GroupLeader = proposal.Student!,
            MatchedSupervisor = matchedSupervisor,
            GroupMembers = acceptedMembers.Where(member => member.Student != null).Select(member => member.Student!).ToList(),
            PendingInvitations = pendingMembers,
            AvailableStudentsToAdd = availableStudents,
            ChatMessages = chatMessages,
            IsCurrentUserGroupLeader = isGroupLeader,
            IsCurrentUserParticipant = canAccess,
            IsChatEnabled = isChatEnabled,
            ProgressStage = progressStage
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvite(AcceptGroupInviteForm form)
    {
        var currentUser = await GetCurrentUserAsync();

        if (currentUser.Role != UserRole.Student)
        {
            TempData["ErrorMessage"] = "Only students can accept group invitations.";
            return RedirectToRoleDashboard(currentUser.Role);
        }

        var invitation = await _dbContext.ProposalGroupMembers
            .FirstOrDefaultAsync(item => item.Id == form.InvitationId && item.StudentId == currentUser.Id);

        if (invitation == null)
        {
            TempData["ErrorMessage"] = "Invitation not found.";
            return RedirectToAction(nameof(Index));
        }

        if (invitation.InviteStatus == GroupInviteStatus.Accepted)
        {
            return RedirectToAction(nameof(Details), new { proposalId = invitation.ProposalId });
        }

        invitation.InviteStatus = GroupInviteStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        TempData["StatusMessage"] = "Invitation accepted. You can now access the project space and chat.";
        return RedirectToAction(nameof(Details), new { proposalId = invitation.ProposalId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostMessage(PostProjectChatMessageForm form)
    {
        var currentUser = await GetCurrentUserAsync();
        var accessCheck = await CheckProjectAccessAsync(form.ProposalId, currentUser.Id);
        if (!accessCheck.CanAccess)
        {
            TempData["ErrorMessage"] = "You cannot post in this project chat.";
            return RedirectToRoleDashboard(currentUser.Role);
        }

        if (!accessCheck.IsMatched)
        {
            TempData["ErrorMessage"] = "Chat opens only after the supervisor confirms the project match.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        var messageText = form.MessageText.Trim();
        if (string.IsNullOrWhiteSpace(messageText))
        {
            TempData["ErrorMessage"] = "Message cannot be empty.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        await _dbContext.ProjectChatMessages.AddAsync(new ProjectChatMessage
        {
            ProposalId = form.ProposalId,
            SenderId = currentUser.Id,
            MessageText = messageText,
            SentAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(AddGroupMemberForm form)
    {
        var currentUser = await GetCurrentUserAsync();

        var proposal = await _dbContext.Proposals.FirstOrDefaultAsync(item => item.Id == form.ProposalId);
        if (proposal == null)
        {
            TempData["ErrorMessage"] = "Project not found.";
            return RedirectToRoleDashboard(currentUser.Role);
        }

        if (proposal.StudentId != currentUser.Id)
        {
            TempData["ErrorMessage"] = "Only the group leader can add members.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        var candidate = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == form.StudentId && user.IsActive && user.Role == UserRole.Student);
        if (candidate == null)
        {
            TempData["ErrorMessage"] = "Selected student is invalid.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        if (candidate.Id == proposal.StudentId)
        {
            TempData["ErrorMessage"] = "Group leader is already in the project.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        var exists = await _dbContext.ProposalGroupMembers.AnyAsync(item => item.ProposalId == form.ProposalId && item.StudentId == form.StudentId);
        if (exists)
        {
            TempData["StatusMessage"] = "Student is already in the project group.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        await _dbContext.ProposalGroupMembers.AddAsync(new ProposalGroupMember
        {
            ProposalId = form.ProposalId,
            StudentId = form.StudentId,
            InviteStatus = GroupInviteStatus.Pending,
            AddedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = "Invitation sent to selected student. They must accept before joining the project space.";
        return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(RemoveGroupMemberForm form)
    {
        var currentUser = await GetCurrentUserAsync();

        var proposal = await _dbContext.Proposals.FirstOrDefaultAsync(item => item.Id == form.ProposalId);
        if (proposal == null)
        {
            TempData["ErrorMessage"] = "Project not found.";
            return RedirectToRoleDashboard(currentUser.Role);
        }

        if (proposal.StudentId != currentUser.Id)
        {
            TempData["ErrorMessage"] = "Only the group leader can remove members.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        var member = await _dbContext.ProposalGroupMembers
            .FirstOrDefaultAsync(item => item.ProposalId == form.ProposalId && item.StudentId == form.StudentId);

        if (member == null)
        {
            TempData["ErrorMessage"] = "Selected student is not in the project group.";
            return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
        }

        _dbContext.ProposalGroupMembers.Remove(member);
        await _dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = member.InviteStatus == GroupInviteStatus.Pending
            ? "Pending invitation removed."
            : "Student removed from project group.";
        return RedirectToAction(nameof(Details), new { proposalId = form.ProposalId });
    }

    private async Task<User> GetCurrentUserAsync()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        return (await _dbContext.Users.FirstAsync(user => user.Id == userId))!;
    }

    private async Task<(bool CanAccess, bool IsMatched)> CheckProjectAccessAsync(int proposalId, int userId)
    {
        var proposal = await _dbContext.Proposals.FirstOrDefaultAsync(item => item.Id == proposalId);
        if (proposal == null)
        {
            return (false, false);
        }

        var isLeader = proposal.StudentId == userId;
        var isMember = await _dbContext.ProposalGroupMembers.AnyAsync(item =>
            item.ProposalId == proposalId &&
            item.StudentId == userId &&
            item.InviteStatus == GroupInviteStatus.Accepted);
        var isMatchedSupervisor = await _dbContext.Matches.AnyAsync(item => item.ProposalId == proposalId && item.SupervisorId == userId && item.Status == MatchStatus.Matched);
        var isMatched = await _dbContext.Matches.AnyAsync(item => item.ProposalId == proposalId && item.Status == MatchStatus.Matched);

        return (isLeader || isMember || isMatchedSupervisor, isMatched);
    }

    private IActionResult RedirectToRoleDashboard(UserRole role)
    {
        return role switch
        {
            UserRole.Student => RedirectToAction("Dashboard", "Student"),
            UserRole.Supervisor => RedirectToAction("Dashboard", "Supervisor"),
            _ => RedirectToAction("Login", "Account")
        };
    }
}
