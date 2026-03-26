using System.Net.Http;
using System.Text.Json;

public class CurrencyService
{
    private readonly HttpClient _httpClient;

    public CurrencyService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetUsdRateAsync()
    {
        try
        {
            // CBU API'dan ma'lumot olish
            var response = await _httpClient.GetStringAsync("https://cbu.uz/uz/arkhiv-kursov-valyut/json/");

            // JSONni parse qilish
            using JsonDocument doc = JsonDocument.Parse(response);
            JsonElement root = doc.RootElement;

            // USD (Dollar) ni qidirib topish
            foreach (var element in root.EnumerateArray())
            {
                if (element.GetProperty("Ccy").GetString() == "USD")
                {
                    string rate = element.GetProperty("Rate").GetString();
                    string diff = element.GetProperty("Diff").GetString();
                    string date = element.GetProperty("Date").GetString();

                    return $"🇺🇸 1 USD = {rate} UZS\n📈 O'zgarish: {diff}\n📅 Sana: {date}";
                }
            }
            return "Kurs ma'lumotlarini topib bo'lmadi.";
        }
        catch (Exception ex)
        {
            return "Xatolik yuz berdi: " + ex.Message;
        }
    }
}
