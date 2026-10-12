using Razor.Templating.Core;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service handles email template rendering using Razor.
 * It provides methods to generate email content for various scenarios.
 */
public interface IEmailService
{
    Task<string> SimpleTitleMessage(SimpleTitleMessageVm simpleTitleMessageVm);
    Task<string> LoginAlert(LoginAlertVm loginAlertVm);
    Task<string> Token(TokenVm tokenVm);
    Task<string> ResetPassword(ResetPasswordVm resetPasswordVm);
    Task<string> PasswordChanged(PasswordChangedVm passwordChangedVm);
    Task<string> IllegalLogin(IllegalLoginVm illegalLoginVm);
    Task<string> AccountCreatedByAdmin(AccountCreatedByAdminVm accountCreatedByAdminVm);
    Task<string> ResetMfa(ResetMfaVm resetMfaVm);
    Task<string> PaymentReceipt(ReceiptVm receiptVm);
    Task<string> OrderCancelRequest(ReceiptVm receiptVm);
    Task<string> OrderRefunded(ReceiptVm receiptVm);
}

public class EmailService : IEmailService
{
    private const string TemplatePath = "~/Views/Email/";
    private readonly IRazorTemplateEngine _engine;

    public EmailService(IRazorTemplateEngine engine)
    {
        _engine = engine;
    }

    public async Task<string> SimpleTitleMessage(SimpleTitleMessageVm simpleTitleMessageVm)
    {
        return await RenderPartialAsync("_SimpleTitleMessage", simpleTitleMessageVm);
    }

    public async Task<string> LoginAlert(LoginAlertVm loginAlertVm)
    {
        return await RenderPartialAsync("_LoginAlert", loginAlertVm);
    }

    public async Task<string> Token(TokenVm tokenVm)
    {
        return await RenderPartialAsync("_Token", tokenVm);
    }

    public async Task<string> ResetPassword(ResetPasswordVm resetPasswordVm)
    {
        return await RenderPartialAsync("_ResetPassword", resetPasswordVm);
    }

    public async Task<string> PasswordChanged(PasswordChangedVm passwordChangedVm)
    {
        return await RenderPartialAsync("_PasswordChanged", passwordChangedVm);
    }

    public async Task<string> IllegalLogin(IllegalLoginVm illegalLoginVm)
    {
        return await RenderPartialAsync("_IllegalLogin", illegalLoginVm);
    }

    public async Task<string> AccountCreatedByAdmin(AccountCreatedByAdminVm accountCreatedByAdminVm)
    {
        return await RenderPartialAsync("_AccountCreatedByAdmin", accountCreatedByAdminVm);
    }

    public async Task<string> ResetMfa(ResetMfaVm resetMfaVm)
    {
        return await RenderPartialAsync("_ResetMfa", resetMfaVm);
    }

    public async Task<string> PaymentReceipt(ReceiptVm receiptVm)
    {
        return await RenderPartialAsync("_PaymentReceipt", receiptVm);
    }

    public async Task<string> OrderCancelRequest(ReceiptVm receiptVm)
    {
        return await RenderPartialAsync("_OrderCancelRequest", receiptVm);
    }

    public async Task<string> OrderRefunded(ReceiptVm receiptVm)
    {
        return await RenderPartialAsync("_OrderRefunded", receiptVm);
    }

    private async Task<string> RenderPartialAsync(string viewName, object model)
    {
        return await _engine.RenderPartialAsync(TemplatePath + viewName + ".cshtml", model);
    }
}