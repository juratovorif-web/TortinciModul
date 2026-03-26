
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RamazonInlineBot
{
    public class RamazonDay
    {
        public int Kun { get; set; }
        public string Sana { get; set; }
        public string Saharlik { get; set; }
        public string Iftorlik { get; set; }
    }

    class Program
    {
        private static ITelegramBotClient botClient;
        private static List<RamazonDay> taqvim = new List<RamazonDay>();

        static async Task Main(string[] args)
        {
            botClient = new TelegramBotClient("8311929927:AAGsLfWUPRuJPBAigTbGK3jJ0PtjI0Jgs28");
            TaqvimniYuklash();

            using var cts = new CancellationTokenSource();
            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions(), cts.Token);

            Console.WriteLine("Bot Inline rejimda ishga tushdi...");
            await Task.Delay(-1);
        }

        static void TaqvimniYuklash()
        {
            DateTime sana = new DateTime(2026, 2, 19);
            for (int i = 1; i <= 30; i++)
            {
                taqvim.Add(new RamazonDay
                {
                    Kun = i,
                    Sana = sana.ToString("dd-MMMM"), // Masalan: 19-Fevral
                    Saharlik = "05:10", // Taxminiy vaqtlar
                    Iftorlik = "18:20"
                });
                sana = sana.AddDays(1);
            }
        }

        static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            // 1. ODDIY XABARLAR (Asosiy menyu)
            if (update.Message is { Text: { } messageText })
            {
                long chatId = update.Message.Chat.Id;

                if (messageText == "/start" || messageText == "⬅️ Asosiy menyu")
                {
                    var menu = new ReplyKeyboardMarkup(new[] {
                new KeyboardButton[] { "🌙 Ramazon Taqvimi 2026" },
                
            })
                    { ResizeKeyboard = true };

                    await bot.SendMessage(chatId, "Asosiy menyu:", replyMarkup: menu, cancellationToken: ct);
                }
                else if (messageText == "🌙 Ramazon Taqvimi 2026")
                {
                    // Sanalar ro'yxatini yuboramiz
                    await bot.SendMessage(chatId, "Kerakli sanani tanlang:", replyMarkup: GetInlineCalendar(), cancellationToken: ct);
                }
            }

            // 2. INLINE TUGMA BOSILGANDA (Sana tanlanganda)
            if (update.CallbackQuery is { } callback)
            {
                long chatId = callback.Message.Chat.Id;
                int messageId = callback.Message.MessageId; // Bu o'sha Inline tugmalar turgan xabar ID-si

                if (callback.Data.StartsWith("day_"))
                {
                    // A) ESKI XABARNI (Tugmalarni) O'CHIRISH
                    try
                    {
                        await bot.DeleteMessage(chatId, messageId, cancellationToken: ct);
                    }
                    catch { /* Xabar allaqachon o'chirilgan bo'lsa xato bermasligi uchun */ }

                    // B) MA'LUMOTNI TAYYORLASH
                    int kunNo = int.Parse(callback.Data.Split('_')[1]);
                    var d = taqvim.FirstOrDefault(x => x.Kun == kunNo);

                    if (d != null)
                    {
                        string text = $"🌙 *Ramazon - {d.Kun} kun*\n📅 Sana: *{d.Sana}*\n\n" +
                                     $"⏳ Saharlik (Yopish): *{d.Saharlik}*\n" +
                                     $"🍴 Iftorlik (Ochish): *{d.Iftorlik}*\n\n" +
                                     $"🤲 *Saharlik duosi:*\n_Навайту ан асума совма шаҳри рамазона минал фажри илал мағриби..._\n\n" +
                                     $"🤲 *Iftorlik duosi:*\n_Аллоҳумма лака сумту ва бика аманту..._";

                        // C) YANGI XABAR SIFATIDA YUBORISH
                        // Bu yerda 'replyMarkup: null' yoki 'GetInlineCalendar()' qilsangiz bo'ladi. 
                        // Agar yana boshqa sanani ko'rishni xohlasa, yana "🌙 Ramazon Taqvimi 2026" tugmasini bosadi.
                        await bot.SendMessage(chatId, text, parseMode: ParseMode.Markdown, cancellationToken: ct);
                    }
                }

                // Telegram tugma "yopishib" qolmasligi uchun javob qaytaramiz
                await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            }
        }

        // Inline tugmalarni (Kalendar ko'rinishida) yasash
        static InlineKeyboardMarkup GetInlineCalendar()
        {
            var rows = new List<List<InlineKeyboardButton>>();
            for (int i = 0; i < 30; i += 3)
            {
                var row = new List<InlineKeyboardButton>();
                for (int j = 0; j < 3; j++)
                {
                    int dayIndex = i + j;
                    if (dayIndex < 30)
                    {
                        var day = taqvim[dayIndex];
                        row.Add(InlineKeyboardButton.WithCallbackData(day.Sana, $"day_{day.Kun}"));
                    }
                }
                rows.Add(row);
            }
            return new InlineKeyboardMarkup(rows);
        }

        static Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine("Xato: " + ex.Message);
            return Task.CompletedTask;
        }
    }
}