using CurrencyBot;
using System.Text.Json;
using Telegram.Bot.Types;

namespace _4_6_dars_TelegramBot.Services;

public class FileService
{
    private const string UserFilePath = "users.json";

    // Bu yerda ham AppUser ishlatamiz
    public async Task SaveUserAsync(AppUser user)
    {
        var users = await GetAllUsersAsync();

        // Endi ChatId aniq ko'rinadi
        if (!users.Any(u => u.ChatId == user.ChatId))
        {
            users.Add(user);
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(UserFilePath, json);
        }
    }

    public async Task<List<AppUser>> GetAllUsersAsync()
    {
        if (!File.Exists(UserFilePath)) return new List<AppUser>();
        var json = await File.ReadAllTextAsync(UserFilePath);

        // Deserialize qilayotganda ham AppUser ishlating
        return JsonSerializer.Deserialize<List<AppUser>>(json) ?? new List<AppUser>();
    }
}
