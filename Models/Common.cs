namespace HotelReservationSystem.Models;

public static class Common
{
    public const string CompanyName = "JX & KQ Hotel";

    // Format
    public const string PhoneNumber = @"^(\+)?[0-9]{10,15}$";
    public const int PasswordLength = 8;

    // Error Message
    public const string ErrorMessage = "Invalid {0}.";
    public const string InvalidRequestErrorMessage = "Invalid request.";

    public const string PasswordErrorMessage = "Password must be at least 8 characters long.";
    public const string PasswordDoesNotMatchErrorMessage = "Password does not match.";

    public const string EmailExistsErrorMessage = "Email already exists.";
    public const string PhoneNumberErrorMessage = "Invalid {0}";

    public const string PleaseLoginToContinueErrorMessage = "Please login to continue.";

    // Room Error Message
    public const string InvalidCheckInOutDateErrorMessage = "Invalid check-in or check-out date.";
    public const string InvalidRoomCategoryErrorMessage = "Invalid room category.";
    public const string NoAvailableRoomErrorMessage = "No available room matches the specified criteria.";

    // Room Constants
    public const decimal AddBedPrice = 50;
    public const decimal CornerRoomPrice = 100;

    // Order Constants
    public const int AllowDaysToCancel = 7;

    // Check In & Out Time
    public const int CheckInTime = 12;
    public const int CheckOutTime = 12;

    // WWWRoot Path
    public const string ImagePath = "images/";
    public const string ImageUploadPath = "uploads/";
}

public static class AuthorizationPolicies
{
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string RequireAdminOrHigher = "RequireAdminOrHigher";
    public const string AuthenticatedUsersOnly = "AuthenticatedUsersOnly";
    public const string UserOnly = "UserOnly";
}

public static class PaymentInit
{
    public const string Currency = "myr";
    public const decimal TaxRate = 8;
    public const decimal ServiceCharge = 10;
}

public static class SweetAlert
{
    public static readonly string Info = "Info";
    public static readonly string Warning = "Warning";
    public static readonly string Success = "Success";
    public static readonly string Danger = "Danger";
}

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";

    public const string Admin = "Admin";

    //public const string Manager = "Manager";
    //public const string Staff = "Staff";
    public const string User = "User";
}

public static class Preference
{
    public const string SmokingArea = "SmokingArea";
    public const string LandscapeView = "LandscapeView";
}

/**
 * @author: Chong Jun Xiang
 * @description: Email design for OTP, Reset Password, Illegal Login, Password Changed
 */
public static class EmailDesign
{
    public const string TokenSubject = "Token";
    public const string ResetPasswordSubject = "Reset Password";
    public const string IllegalLoginSubject = "Illegal Login";
    public const string PasswordChangedSubject = "Password Changed";
    public const string AccountCreatedSubject = "Account Created";
    public const string LoginAlertSubject = "Login Alert";
    public const string MfaAlertSubject = "MFA Alert";
    public const string ResetMfaSubject = "Reset MFA";
    public const string PaymentReceiptSubject = "Payment Receipt";
    public const string StripePaymentReceiptSubject = "Stripe";
    public const string OrderCancelRequestSubject = "Order Cancel Request";
    public const string OrderRefundedSubject = "Order Refunded";
}
