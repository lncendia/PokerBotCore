using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using File = System.IO.File;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class ChangeTableQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            var image = Image.FromFile(File.Exists($"tables\\{user.Id}.jpg")
                ? $"tables\\{user.Id}.jpg"
                : $"tables\\table.jpg");
            await using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Jpeg);
                image.Dispose();
                ms.Position = 0;
                await client.SendPhotoAsync(query.From.Id, new InputOnlineFile(ms),
                    caption: "Ваш нынешний фон.");
                await client.SendTextMessageAsync(query.From.Id,
                    "Отправьте фотографию фона, который хотите установить.",
                    replyMarkup: MainKeyboards.StandartTable);
            }

            user.state = State.enterPhotoTable;
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "change_table" && user.state == State.main;
        }
    }
}