namespace HotelReservationSystem.Utilities;

public static class HttpHelper
{
    /**
     * @author: Chong Jun Xiang
     * @description: Get HTML content from URL
     */
    public static async Task<string> GetHtmlWithUrl(string url)
    {
        using HttpClient client = new HttpClient();
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string receiptContent = await response.Content.ReadAsStringAsync();
            return receiptContent;
        }
        catch
        {
            return string.Empty;
        }
    }
}