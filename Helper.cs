using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelReservationSystem;

public class Helper
{
    private static readonly PasswordHasher<object> PasswordHasher = new();
    private readonly IConfiguration _cf;
    private readonly IHttpContextAccessor _ct;

    private readonly IWebHostEnvironment _en;

    public Helper(IWebHostEnvironment en, IHttpContextAccessor ct, IConfiguration cf)
    {
        _en = en;
        _ct = ct;
        _cf = cf;
    }

    public static string HashPassword(string password)
    {
        return PasswordHasher.HashPassword(0, password);
    }

    public static bool VerifyPassword(string hash, string password)
    {
        return PasswordHasher.VerifyHashedPassword(0, hash, password) == PasswordVerificationResult.Success;
    }

    // Return January (1) to December (12)
    public static SelectList GetMonthList()
    {
        List<object> list = new();

        for (int n = 1; n <= 12; n++)
            list.Add(new
            {
                Id = n,
                Name = new DateTime(1, n, 1).ToString("MMMM")
            });

        return new SelectList(list, "Id", "Name");
    }

    // Return min to max years
    public static SelectList GetYearList(int min, int max, bool reverse = false)
    {
        List<int> list = new();

        for (int n = min; n <= max; n++) list.Add(n);

        if (reverse) list.Reverse();

        return new SelectList(list);
    }

    public static string Encrypt<T>(T data)
    {
        byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(data);
        return Convert.ToBase64String(jsonData);
    }

    public static T? Decrypt<T>(string data)
    {
        byte[] encrypted = Convert.FromBase64String(data);
        return JsonSerializer.Deserialize<T>(encrypted);
    }

    public static string GetRandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string randomString = "";
        Random random = new();
        for (int i = 1; i <= length; i++) randomString += chars[random.Next(chars.Length)];
        return randomString;
    }
}