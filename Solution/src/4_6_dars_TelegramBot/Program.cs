using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CurrencyBot
{
    // --- MODELLAR ---
    public class AppUser
    {
        public long ChatId { get; set; }
        public string Name { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsRegistered { get; set; } = false;
    }

    public class UserLog
    {
        public long ChatId { get; set; }
        public DateTime RequestTime { get; set; }
    }

    // --- ASOSIY PROGRAMMA ---
    class Program
    {
        private static ITelegramBotClient botClient;
        private const string UserFile = "users.json";
        private const string LogFile = "logs.json";
        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            // Telegram bot tokeningizni bura yozing
            botClient = new TelegramBotClient("8738970736:AAG9lhT2J78N6Kv-bLRDNi0CXaykXf71ZYA");

            using var cts = new CancellationTokenSource();

            // Botni ishga tushirish
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
                cancellationToken: cts.Token
            );

            Console.WriteLine("Bot muvaffaqiyatli ishga tushdi...");
            await Task.Delay(-1); // Bot o'chib qolmasligi uchun
        }

        static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            // Faqat xabarlarni va kontaktlarni qabul qilamiz
            if (update.Message is not { } message) return;

            long chatId = message.Chat.Id;
            string? text = message.Text;

            var users = LoadData<AppUser>(UserFile);
            var currentUser = users.FirstOrDefault(u => u.ChatId == chatId);

            // 1. RO'YXATDAN O'TISHNI TEKSHIRISH
            if (currentUser == null || !currentUser.IsRegistered)
            {
                if (message.Type == MessageType.Contact && message.Contact != null)
                {
                    var newUser = new AppUser
                    {
                        ChatId = chatId,
                        Name = message.Contact.FirstName,
                        LastName = message.Contact.LastName, // null bo'lishi mumkin
                        PhoneNumber = message.Contact.PhoneNumber,
                        IsRegistered = true
                    };
                    users.Add(newUser);
                    SaveData(UserFile, users);

                    await bot.SendMessage(chatId, "Rahmat! Ro'yxatdan o'tdingiz.", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
                    await ShowMainMenu(chatId, ct);
                }
                else
                {
                    var contactBtn = new ReplyKeyboardMarkup(new[]
                    {
                        KeyboardButton.WithRequestContact("Ro'yxatdan o'tish (Kontakt yuborish)")
                    })
                    { ResizeKeyboard = true };

                    await bot.SendMessage(chatId, "Assalomu alaykum! Botni ishlatish uchun telefon raqamingizni yuboring:", replyMarkup: contactBtn, cancellationToken: ct);
                }
                return;
            }

            // 2. ASOSIY MENYU BUYRUQLARI
            if (text == "Kursni ko'rish")
            {
                string rate = await GetCurrencyFromApi();
                await bot.SendMessage(chatId, rate, cancellationToken: ct);

                // Statistikani yozish
                LogActivity(chatId);
            }
            else if (text == "Statistika")
            {
                var logs = LoadData<UserLog>(LogFile);
                var dailyCount = logs.Count(l => l.ChatId == chatId && l.RequestTime.Date == DateTime.Today);

                await bot.SendMessage(chatId, $"Siz bugun {dailyCount} marta kursni so'radingiz.", cancellationToken: ct);
            }
            else
            {
                await ShowMainMenu(chatId, ct);
            }
        }

        // --- YORDAMCHI FUNKSIYALAR ---

        static async Task ShowMainMenu(long chatId, CancellationToken ct)
        {
            var menu = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Kursni ko'rish", "Statistika" }
            })
            { ResizeKeyboard = true };

            await botClient.SendMessage(chatId, "Menyuni tanlang:", replyMarkup: menu, cancellationToken: ct);
        }

        static async Task<string> GetCurrencyFromApi()
        {
            try
            {
                var response = await httpClient.GetStringAsync("https://cbu.uz/uz/arkhiv-kursov-valyut/json/");
                using JsonDocument doc = JsonDocument.Parse(response);
                var usd = doc.RootElement.EnumerateArray().FirstOrDefault(x => x.GetProperty("Ccy").GetString() == "USD");

                string rate = usd.GetProperty("Rate").GetString();
                string date = usd.GetProperty("Date").GetString();

                return $"🇺🇸 1 USD = {rate} UZS\n📅 Sana: {date}";
            }
            catch { return "Xatolik: Kursni olib bo'lmadi."; }
        }

        static void LogActivity(long chatId)
        {
            var logs = LoadData<UserLog>(LogFile);
            logs.Add(new UserLog { ChatId = chatId, RequestTime = DateTime.Now });
            SaveData(LogFile, logs);
        }

        static List<T> LoadData<T>(string fileName)
        {
            if (!File.Exists(fileName)) return new List<T>();
            string json = File.ReadAllText(fileName);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }

        static void SaveData<T>(string fileName, List<T> data)
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fileName, json);
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine("Xato: " + exception.Message);
            return Task.CompletedTask;
        }
    }
}