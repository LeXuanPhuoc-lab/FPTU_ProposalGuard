namespace FPTU_ProposalGuard.Application.Common;

public class ResultCodeConst
{
    #region System
    /// <summary>
    /// [SUCCESS] Create success
    /// </summary> 
    public static string SYS_Success0001 = "SYS.Success0001";

    /// <summary>
    /// [SUCCESS] Read success
    /// </summary> 
    public static string SYS_Success0002 = "SYS.Success0002";

    /// <summary>
    /// [SUCCESS] Update success
    /// </summary> 
    public static string SYS_Success0003 = "SYS.Success0003";

    /// <summary>
    /// [SUCCESS] Delete success
    /// </summary> 
    public static string SYS_Success0004 = "SYS.Success0004";

    /// <summary>
    /// [SUCCESS] Import (number) data successfully
    /// </summary> 
    public static string SYS_Success0005 = "SYS.Success0005";

    /// <summary>
    /// [SUCCESS] Create new {0} successfully
    /// </summary> 
    public static string SYS_Success0006 = "SYS.Success0006";

    /// <summary>
    /// [SUCCESS] Deleted data to trash
    /// </summary> 
    public static string SYS_Success0007 = "SYS.Success0007";

    /// <summary>
    /// [SUCCESS] Deleted {0} data successfully
    /// </summary> 
    public static string SYS_Success0008 = "SYS.Success0008";

    /// <summary>
    /// [SUCCESS] Recovery data successfully
    /// </summary> 
    public static string SYS_Success0009 = "SYS.Success0009";

    /// <summary>
    /// [FAIL] Create fail
    /// </summary> 
    public static string SYS_Fail0001 = "SYS.Fail0001";

    /// <summary>
    /// [FAIL] Read fail
    /// </summary>
    public static string SYS_Fail0002 = "SYS.Fail0002";

    /// <summary>
    /// [FAIL] Update fail
    /// </summary>
    public static string SYS_Fail0003 = "SYS.Fail0003";

    /// <summary>
    /// [FAIL] Delete fail
    /// </summary>
    public static string SYS_Fail0004 = "SYS.Fail0004";

    /// <summary>
    /// [FAIL] Fail to get data: {0}
    /// </summary>
    public static string SYS_Fail0005 = "SYS.Fail0005";

    /// <summary>
    /// [FAIL] Fail to create new {0}
    /// </summary>
    public static string SYS_Fail0006 = "SYS.Fail0006";

    /// <summary>
    /// [FAIL] Unable to delete because it is bound to other data
    /// </summary>
    public static string SYS_Fail0007 = "SYS.Fail0007";

    /// <summary>
    /// [FAIL] Fail to import data
    /// </summary>
    public static string SYS_Fail0008 = "SYS.Fail0008";

    /// <summary>
    /// [FAIL] Recovery data failed
    /// </summary> 
    public static string SYS_Fail0009 = "SYS.Fail0009";

    /// <summary>
    /// [WARNING] Invalid inputs
    /// </summary>
    public static string SYS_Warning0001 = "SYS.Warning0001";

    /// <summary>
    /// [WARNING] Not found {0}
    /// </summary>
    public static string SYS_Warning0002 = "SYS.Warning0002";

    /// <summary>
    /// [WARNING] Unable to progress {0} as {1} already exist
    /// </summary>
    public static string SYS_Warning0003 = "SYS.Warning0003";

    /// <summary>
    /// [WARNING] Data not found or empty
    /// </summary>
    public static string SYS_Warning0004 = "SYS.Warning0004";

    /// <summary>
    /// [WARNING] No data effected
    /// </summary>
    public static string SYS_Warning0005 = "SYS.Warning0005";

    /// <summary>
    /// [WARNING] Unknown error
    /// </summary>
    public static string SYS_Warning0006 = "SYS.Warning0006";

    /// <summary>
    /// [WARNING] Unable to delete {0} as it is in use
    /// </summary>
    public static string SYS_Warning0007 = "SYS.Warning0007";

    /// <summary>
    /// [WARNING] Unable to edit because it is bound to other data
    /// </summary>
    public static string SYS_Warning0008 = "SYS.Warning0008";
    #endregion
    
    #region Auth
    /// <summary>
	/// [SUCCESS] Create account success 
	/// </summary>
	public static string Auth_Success0001 = "Auth.Success0001";
	/// <summary>
	/// [SUCCESS] Sign-in success 
	/// </summary>
	public static string Auth_Success0002 = "Auth.Success0002";
	/// <summary>
	/// [SUCCESS] Sign-in with password 
	/// </summary>
	public static string Auth_Success0003 = "Auth.Success0003";
	/// <summary>
	/// [SUCCESS] Sign-in with OTP 
	/// </summary>
	public static string Auth_Success0004 = "Auth.Success0004";
	/// <summary>
	/// [SUCCESS] Send OPT to email success
	/// </summary>
	public static string Auth_Success0005 = "Auth.Success0005";
	/// <summary>
	/// [SUCCESS] Change password success 
	/// </summary>
	public static string Auth_Success0006 = "Auth.Success0006";
	/// <summary>
	/// [SUCCESS] Confirm account success
	/// </summary>
	public static string Auth_Success0007 = "Auth.Success0007";
	/// <summary>
	/// [SUCCESS] Create new Token successfully
	/// </summary>
	public static string Auth_Success0008 = "Auth.Success0008";
	/// <summary>
	/// [SUCCESS] Email verification success, please check email to reset password
	/// </summary>
	public static string Auth_Success0009 = "Auth.Success0009";
	/// <summary>
	/// [SUCCESS] Create MFA backup codes succesfully
	/// </summary>
	public static string Auth_Success0010 = "Auth.Success0010";
	
	/// <summary>
	/// [WARNING] Account not allow to access
	/// </summary>
	public static string Auth_Warning0001 = "Auth.Warning0001";
	/// <summary>
	/// [WARNING] Invalid token
	/// </summary>
	public static string Auth_Warning0002 = "Auth.Warning0002";
	/// <summary>
	/// [WARNING] Invalid token with detail message
	/// </summary>
	public static string Auth_Warning0003 = "Auth.Warning0003";
	/// <summary>
	/// [WARNING] Account already confirmed
	/// </summary>
	public static string Auth_Warning0004 = "Auth.Warning0004";
	/// <summary>
	/// [WARNING] OTP code is incorrect, please resend
	/// </summary>
	public static string Auth_Warning0005 = "Auth.Warning0005";
	/// <summary>
	/// [WARNING] Email already exist
	/// </summary>
	public static string Auth_Warning0006 = "Auth.Warning0006";
	/// <summary>
	/// [WARNING] Wrong username or password
	/// </summary>
	public static string Auth_Warning0007 = "Auth.Warning0007";
	/// <summary>
	/// [WARNING] Account email is not verify yet
	/// </summary>
	public static string Auth_Warning0008 = "Auth.Warning0008";
	/// <summary>
	/// [WARNING] The account has enabled 2-factor
	/// </summary>
	public static string Auth_Warning0009 = "Auth.Warning0009";
	/// <summary>
	/// [WARNING] Two-factor authentication is required
	/// </summary>
	public static string Auth_Warning0010 = "Auth.Warning0010";
	/// <summary>
	/// [WARNING] Unable to process as the account has not enabled 2-factor authentication yet
	/// </summary>
	public static string Auth_Warning0011 = "Auth.Warning0011";
	/// <summary>
	/// [WARNING] Backup code is not valid
	/// </summary>
	public static string Auth_Warning0012 = "Auth.Warning0012";
	/// <summary>
	/// [WARNING] Please sign in to access this feature
	/// </summary>
	public static string Auth_Warning0013 = "Auth.Warning0013";
	
	/// <summary>
	/// [FAIL] Fail to update password
	/// </summary>
	public static string Auth_Fail0001 = "Auth.Fail0001";
	/// <summary>
	/// [FAIL] Fail to send OTP 
	/// </summary>
	public static string Auth_Fail0002 = "Auth.Fail0002";
	/// <summary>
	/// [FAIL] Fail to create backup codes
	/// </summary>
	public static string Auth_Fail0003 = "Auth.Fail0003";
    #endregion
    
    #region Notification
    /// <summary>
    /// [SUCCESS] Send notification successfully
    /// </summary>
    public static string Notification_Success0001 = "Notification.Success0001";
    /// <summary>
    /// [WARNING] There is no important message 
    /// </summary>
    public static string Notification_Warning0001 = "Notification.Warning0001";
    /// <summary>
    /// [WARNING] Access to noti that not belongs to this account 
    /// </summary>
    public static string Notification_Warning0002 = "Notification.Warning0002";
    /// <summary>
    /// [WARNING] Not found user's email {0} to process send privacy notification
    /// </summary>
    public static string Notification_Warning0003 = "Notification.Warning0003";
    /// <summary>
    /// [FAIL] Failed to send notification
    /// </summary>
    public static string Notification_Fail0001 = "Notification.Fail0001";
    #endregion

    #region Proposal
	
    /// <summary>
    /// [SUCCESS] Get Data in file successfully
    /// </summary>
    public static string Proposal_Success0001 = "Proposal.Success0001";
    /// <summary>
    /// [SUCCESS] Upload data to OpenSearch successfully
    /// </summary>
    public static string Proposal_Success0002 = "Proposal.Success0002";
    /// <summary>
    /// [SUCCESS] Add reviewer successfully
    /// </summary>
    public static string Proposal_Success0003 = "Proposal.Success0003";
    /// <summary>
    /// [WARNING] Something went wrong while processing the file
    /// </summary>
    public static string Proposal_Warning0001 = "Proposal.Warning0001";
    /// <summary>
    /// [WARNING] OpenSearch Bulk Error
    /// </summary>
    public static string Proposal_Warning0002 = "Proposal.Warning0002";
    /// <summary>
    /// [WARNING] Approved User can not be processed user
    /// </summary>
    public static string Proposal_Warning0003 = "Proposal.Warning0003";
    /// <summary>
    /// [WARNING] Only Pending can be changed to Approved or Rejected	
    /// </summary>
    public static string Proposal_Warning0004 = "Proposal.Warning0004";
    /// <summary>
    /// [WARNING] Some proposals do not have enough reviewers assigned or have not been reviewed yet.
    /// </summary>
    public static string Proposal_Warning0005 = "Proposal.Warning0005";
    #endregion

    #region ProposalStudent
    /// <summary>
    /// [SUCCESS] Create students successfully
    /// </summary>
    public static string ProposalStudent_Success0001 = "ProposalStudent.Success0001";
    
    #endregion

    #region Review
    /// <summary>
    /// [SUCCESS] Submit Review successfully
    /// </summary>
    public static string Review_Success0001 = "Review.Success0001";
    /// <summary>
    /// [SUCCESS] Number of answer is
    /// </summary>
    public static string Review_Warning0001 = "Review.Warning0001";
    
    #endregion
}
