using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.ViewModels;

// Register Page
public class RegisterVm
{
    [DisplayName("First Name")] public string FirstName { get; set; } = string.Empty;

    [DisplayName("Last Name")] public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    [Remote("IsEmailUnique", "User", ErrorMessage = Common.EmailExistsErrorMessage, HttpMethod = "POST")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(Common.PasswordLength, ErrorMessage = Common.PasswordErrorMessage)]
    [Compare("ConfirmPassword", ErrorMessage = Common.PasswordDoesNotMatchErrorMessage)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    [MinLength(Common.PasswordLength, ErrorMessage = Common.PasswordErrorMessage)]
    [Compare("Password", ErrorMessage = Common.PasswordDoesNotMatchErrorMessage)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [DataType(DataType.PhoneNumber)]
    [DisplayName("Phone Number")]
    [StringLength(15, ErrorMessage = Common.ErrorMessage)]
    [RegularExpression(Common.PhoneNumber, ErrorMessage = Common.ErrorMessage)]
    public string PhoneNumber { get; set; } = string.Empty;
}

// Login Page
public class LoginVm
{
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(Common.PasswordLength, ErrorMessage = Common.PasswordErrorMessage)]
    public string Password { get; set; } = string.Empty;

    [DisplayName("Remember me?")] public bool RememberMe { get; set; } = false;
}

// MFA Page
public class LoginMfaVm
{
    [StringLength(6, ErrorMessage = "Invalid {0}")]
    public string Code { get; set; } = string.Empty;
}

// Email Verification Page
public class EmailVerificationVm
{
    public string Token { get; set; } = string.Empty;
}

// Forgot Password Page
public class ForgotPasswordVm
{
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;
}

// Reset Password Page
public partial class ResetPasswordVm
{
    public string Token { get; set; } = string.Empty;

    [EmailAddress] public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [DisplayName("New Password")]
    [MinLength(Common.PasswordLength, ErrorMessage = Common.PasswordErrorMessage)]
    [Compare("ConfirmNewPassword", ErrorMessage = Common.PasswordDoesNotMatchErrorMessage)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [DisplayName("Confirm New Password")]
    [MinLength(Common.PasswordLength, ErrorMessage = Common.PasswordErrorMessage)]
    [Compare("NewPassword", ErrorMessage = Common.PasswordDoesNotMatchErrorMessage)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

// Recovery MFA Page
public class RecoveryMfaVm
{
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;
}

// Reset MFA Page
public partial class ResetMfaVm
{
    // Form
    public string Token { get; set; } = string.Empty;

    [StringLength(6, ErrorMessage = "Invalid {0}")]
    public string Code { get; set; } = string.Empty;

    // Vm
    public string? Email { get; set; }

    public string? QrCodeUrl { get; set; }

    public string? ManualEntryCode { get; set; }
}

public class SendContactUsVm
{
    // Form
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}