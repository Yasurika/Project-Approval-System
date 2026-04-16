using ProjectApprovalSystem.Core.Entities;

namespace ProjectApprovalSystem.Web.Models;

public class LoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class StudentDashboardViewModel
{
    public required User Student { get; init; }
    public required List<ResearchArea> ResearchAreas { get; init; }
    public required List<Proposal> Proposals { get; init; }
    public required string TimelineStage { get; init; }
    public User? MatchedSupervisor { get; init; }
}

public class SubmitStudentProposalForm
{
    public required string Title { get; set; }
    public required string Abstract { get; set; }
    public required string TechStack { get; set; }
    public required string Description { get; set; }
    public int ResearchAreaId { get; set; }
}

public class UpdateStudentProposalForm
{
    public int ProposalId { get; set; }
    public required string Title { get; set; }
    public required string Abstract { get; set; }
    public required string TechStack { get; set; }
    public required string Description { get; set; }
}

public class SupervisorDashboardViewModel
{
    public required User Supervisor { get; init; }
    public required List<ResearchArea> ResearchAreas { get; init; }
    public required HashSet<int> SelectedResearchAreaIds { get; init; }
    public required List<Proposal> AnonymousProposals { get; init; }
    public required List<Match> PendingInterests { get; init; }
    public required List<Match> ConfirmedMatches { get; init; }
    public Match? RevealedMatch { get; init; }
}

public class SaveExpertiseForm
{
    public int[] ResearchAreaIds { get; set; } = [];
}

public class ConfirmMatchFromFeedForm
{
    public int ProposalId { get; set; }
    public string? Comments { get; set; }
}

public class AdminDashboardViewModel
{
    public required User AdminUser { get; init; }
    public int PendingProposalCount { get; init; }
    public int UnderReviewCount { get; init; }
    public int MatchedCount { get; init; }
    public required List<Match> MasterMatches { get; init; }
    public required List<ResearchArea> ResearchAreas { get; init; }
    public required List<User> Users { get; init; }
    public required List<User> Supervisors { get; init; }
}

public class CreateResearchAreaAdminForm
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateResearchAreaAdminForm
{
    public int AreaId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class CreateUserAdminForm
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public UserRole Role { get; set; }
}

public class ReassignMatchAdminForm
{
    public int MatchId { get; set; }
    public int NewSupervisorId { get; set; }
}
