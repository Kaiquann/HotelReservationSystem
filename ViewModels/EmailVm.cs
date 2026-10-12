namespace HotelReservationSystem.ViewModels;

public class SimpleTitleMessageVm
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class LoginAlertVm
{
    public string Email { get; set; } = string.Empty;
    public IpInfoResponse IpInfo { get; set; } = null!;
    public DateTime LoginTime { get; set; } = DateTime.Now;
    public string Url { get; set; } = string.Empty;
}

public class TokenVm
{
    public string Token { get; set; } = string.Empty;
}

public partial class ResetPasswordVm
{
    public string Url { get; set; } = string.Empty;
}

public class PasswordChangedVm
{
    public string DateTime { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class IllegalLoginVm
{
    public string Email { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class AccountCreatedByAdminVm
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public partial class ResetMfaVm
{
    public string Url { get; set; } = string.Empty;
}