namespace ProjectApprovalSystem.Core.Entities;

/// <summary>
/// Enum representing different user types in the system
/// </summary>
public enum UserRole
{
    Student = 1,
    Supervisor = 2,
    ModuleLeader = 3,
    Administrator = 4
}

/// <summary>
/// Enum representing the status of a proposal
/// </summary>
public enum ProposalStatus
{
    Pending = 1,
    Matched = 2,
    Rejected = 3,
    Withdrawn = 4
}

/// <summary>
/// Enum representing the status of a match/interest
/// </summary>
public enum MatchStatus
{
    Interested = 1,
    Matched = 2,
    Declined = 3
}
