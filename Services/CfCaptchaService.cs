using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This class integrates with the Cloudflare captcha service.
 * It provides methods to verify captcha tokens, generate captcha HTML,
 * and validate captcha responses from HTTP requests.
 */
public class CfCaptcha
{
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public class CaptchaResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
}

public interface ICfCaptchaService
{
    bool VerifyCaptcha(string token);
    IHtmlContent GetCaptchaHtml();
    bool IsCaptchaResponseValid(HttpRequest request);
}

public class CfCaptchaService : ICfCaptchaService
{
    private const string CfCaptchaUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private const string CfCaptchaHtml = "<div class='cf-turnstile' data-sitekey='CF_CAPTCHA_SITE_KEY'></div>";
    private const string CfTurnstileResponse = "cf-turnstile-response";

    private readonly CfCaptcha _cfCaptcha;

    public CfCaptchaService(CfCaptcha cfCaptcha)
    {
        _cfCaptcha = cfCaptcha;
    }

    public bool VerifyCaptcha(string token)
    {
        using HttpClient client = new();
        FormUrlEncodedContent content = new([
            new KeyValuePair<string, string>("secret", _cfCaptcha.SecretKey),
            new KeyValuePair<string, string>("response", token)
        ]);
        HttpResponseMessage response = client.PostAsync(CfCaptchaUrl, content).Result;
        string responseContent = response.Content.ReadAsStringAsync().Result;
        CaptchaResponse? captchaResponse = JsonSerializer.Deserialize<CaptchaResponse>(responseContent);
        return captchaResponse != null && captchaResponse.Success;
    }

    public IHtmlContent GetCaptchaHtml()
    {
        return new HtmlString(CfCaptchaHtml.Replace("CF_CAPTCHA_SITE_KEY", _cfCaptcha.SiteKey));
    }

    public bool IsCaptchaResponseValid(HttpRequest request)
    {
        string? turnstileResponse = request.Form[CfTurnstileResponse];
        if (turnstileResponse == null) return false;
        return request.Form.ContainsKey(CfTurnstileResponse) && VerifyCaptcha(turnstileResponse);
    }
}