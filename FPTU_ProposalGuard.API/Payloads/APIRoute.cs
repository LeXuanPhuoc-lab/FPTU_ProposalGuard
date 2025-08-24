namespace FPTU_ProposalGuard.API.Payloads;

public class APIRoute
{
    private const string Base = "api";
    
    /// <summary>
    /// Authentication endpoints
    /// </summary>
    public static class Authentication
    {
        // [GET]
        public const string ForgotPassword = Base + "/auth/forgot-password";
        public const string CurrentUser = Base + "/auth/current-user";
        public const string GetMfaBackupAsync = Base + "/auth/mfa-backup";
        // [POST]
        public const string SignIn = Base + "/auth/sign-in";
        public const string SignInWithPassword = Base + "/auth/sign-in/password-method";
        public const string SignInWithOtp = Base + "/auth/sign-in/otp-method";
        public const string SignInWithGoogle = Base + "/auth/sign-in-google";
        public const string RefreshToken = Base + "/auth/refresh-token";
        public const string ResendOtp = Base + "/auth/resend-otp";
        public const string ChangePasswordOtpVerification = Base + "/auth/change-password/verify-otp";
        public const string EnableMfa = Base + "/auth/enable-mfa";
        public const string ValidateMfa = Base + "/auth/validate-mfa";
        public const string ValidateBackupCode = Base + "/auth/validate-mfa-backup";
        public const string RegenerateBackupCode = Base + "/auth/regenerate-mfa-backup";
        public const string RegenerateBackupCodeConfirm = Base + "/auth/regenerate-mfa-backup/confirm";
        // [PATCH]
        public const string ChangePassword = Base + "/auth/change-password";
        // [PUT]
        public const string UpdateProfile = Base + "/auth/profile";
    }

    /// <summary>
    /// Notification endpoints
    /// </summary>
    public static class Notification
    {
        #region Management
        //	[GET]	
        public const string GetAll = Base + "/management/notifications";
        public const string GetById = Base + "/management/notifications/{id}";
        //	[POST]
        public const string Create = Base + "/management/notifications";
        //	[PUT]
        public const string Update = Base + "/management/notifications/{id}";
        //	[PATCH]
        //	[DELETE]
        #endregion
			
        //	[GET]
        public const string GetPrivacyById = Base + "/privacy/notifications/{id}";
        //	[POST]
        public const string GetAllPrivacy = Base + "/privacy/notifications";
        public const string GetNumberOfUnreadNotifications = Base + "/privacy/unread-noti"; 
        //	[PUT]
        public const string UpdateReadStatus = Base + "/privacy/notifications";
        public const string MarkAsReadAll = Base + "/privacy/notifications/mark-as-read-all";
        //	[PATCH]
        //	[DELETE]
    }

    /// <summary>
    /// User endpoints
    /// </summary>
    public static class User
    {
        // [GET]
        public const string GetById = Base + "/management/users/{id}";
        public const string GetAll = Base + "/management/users";
        public const string Export = Base + "/management/users/export";
        // [POST]
        public const string Create = Base + "/management/users";
        public const string Import = Base + "/management/users/import";
        // [PUT]
        public const string Update = Base + "/management/users/{id}";
        // [PATCH]
        public const string ChangeAccountStatus = Base + "/management/users/{id}/status";
        public const string SoftDelete = Base + "/management/users/{id}/soft-delete";
        public const string SoftDeleteRange = Base + "/management/users/soft-delete-range";
        public const string UndoDelete = Base + "/management/users/{id}/undo-delete";
        public const string UndoDeleteRange = Base + "/management/users/undo-delete-range";
        // [DELETE]
        public const string HardDelete = Base + "/management/users/{id}";
        public const string HardDeleteRange = Base + "/management/users";
    }
    
    /// <summary>
    /// Role endpoints
    /// </summary>
    public static class Role
    {
        //	[GET]
        public const string GetAllRole = Base + "/management/roles";
        public const string GetById = Base + "/management/roles/{id}";
        //	[POST]
        //	[PUT]
        //	[PATCH]
        public const string UpdateUserRole = Base + "/management/roles/users/{userId}";
        //	[DELETE]
        public const string DeleteRole = Base + "/management/roles/{id}";
    }

    /// <summary>
    /// Proposal endpoints
    /// </summary>
    public static class Proposal
    {
        //[GET]
        public const string GetById = Base + "/proposals/{id}";
        public const string GetFile = Base + "/proposals/files/{historyId}";
        public const string GetAll = Base + "/proposals";
        public const string GetAllUploaded = Base + "/proposals/uploaded";
        public const string ExportSemesterReport = Base + "/proposals/export-semester-report";
        //[POST]
        public const string AddProposalsWithFiles = Base + "/proposals/files";
        public const string AddReviewers = Base +"/proposals/reviewers";
        public const string AddProposals = Base + "/proposals";
        public const string CheckDuplicatedProposal = Base + "/proposals/check-duplicate";
        public const string SubmitReview = Base + "/propsoals/{id}/submit-review";
        //[PUT]
        public const string UpdateStatus = Base + "/proposals/{proposalId}/status";
        public const string ReUploadProposal = Base + "/proposals/re-upload/{proposalId}";
        
        
    }
    public static class Question
    {
        // [GET]
        public const string GetAllReviewQuestions = Base + "/review-questions";
        // [POST]
     
        // [PATCH]
       
        // [PUT]
      
    } public static class Semester
    {
        // [GET]
        public const string GetCurrentSemester = Base + "/semesters/current";
        // [POST]
     
        // [PATCH]
       
        // [PUT]
      
    }

    public static class ReviewSession
    {
        // [GET]
        public const string GetSessionsToBeReviewed = Base + "/review-sessions/to-be-reviewed";
        // [POST]

        // [PATCH]

        // [PUT]
    }
}