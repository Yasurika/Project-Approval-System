namespace ProjectApprovalSystem.Core.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public const int MaxProposalsPerStudent = 5;
    public const int MaxTeamSize = 6;
    public const int MinTeamSize = 1;
    
    // Proposal ID format: YYYY-XXXX
    public const string ProposalIdFormat = "yyyy-0000";
    
    // Security
    public const int PasswordMinLength = 8;
    public const bool PasswordRequireNumbers = true;
    public const bool PasswordRequireSpecialChars = true;

    // Messages
    public const string BlindMatchingErrorMessage = "Student identity cannot be revealed before matching";
    public const string InvalidProposalStatusMessage = "Invalid proposal status for this operation";
    public const string ProposalAlreadyMatchedMessage = "Proposal is already matched and cannot be modified";
}

/// <summary>
/// Error codes used throughout the application
/// </summary>
public enum ErrorCode
{
    //User errors
    UserNotFound = 1001,
    InvalidPassword = 1002,
    EmailAlreadyExists = 1003,
    UnauthorizedAccess = 1004,

    // Proposal errors
    ProposalNotFound = 2001,
    ProposalAlreadyMatched = 2002,
    InvalidProposalStatus = 2003,

    // Matching errors
    BlindMatchingCompromised = 3001,
    MatchNotFound = 3002,
    AlreadyInterested = 3003,

    // Generic errors
    DatabaseError = 9001,
    InvalidOperation = 9002
}
