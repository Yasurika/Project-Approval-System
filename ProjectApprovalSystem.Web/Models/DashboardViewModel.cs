using ProjectApprovalSystem.Core.Entities;

namespace ProjectApprovalSystem.Web.Models;

public class DashboardViewModel
{
    public required List<User> Students { get; init; }
    public required List<User> Supervisors { get; init; }
    public required List<User> ModuleLeaders { get; init; }
    public required List<ResearchArea> ResearchAreas { get; init; }
    public required List<Proposal> StudentProposals { get; init; }
    public required List<Proposal> AnonymousProposals { get; init; }
    public required List<SupervisorExpertise> SupervisorExpertises { get; init; }
    public required List<Match> Matches { get; init; }
    public int? SelectedStudentId { get; init; }
    public int? SelectedSupervisorId { get; init; }
}

public class RegisterUserForm
{
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string Password { get; set; }
    public UserRole Role { get; set; }
}

public class CreateResearchAreaForm
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class AssignExpertiseForm
{
    public int SupervisorId { get; set; }
    public int ResearchAreaId { get; set; }
}

public class SubmitProposalForm
{
    public int StudentId { get; set; }
    public required string Title { get; set; }
    public required string Abstract { get; set; }
    public required string Description { get; set; }
    public required string TechStack { get; set; }
    public int ResearchAreaId { get; set; }
}

public class ExpressInterestForm
{
    public int ProposalId { get; set; }
    public int SupervisorId { get; set; }
    public string? Comments { get; set; }
}