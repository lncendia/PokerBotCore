using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterPhotoTableCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (message.Type != MessageType.Photo)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Отправьте фотографию!");
                return;
            }
            
            await using (var ms = new MemoryStream())
            {
                await client.GetInfoAndDownloadFileAsync(message.Photo[^1].FileId, ms);
                Image image = Image.FromStream(ms);
                var bmp = new Bitmap(image, 1590, 960);
                bmp.Save($"tables\\{user.Id}.jpg");
                bmp.Dispose();
                image.Dispose();
            }
            user.state = State.main;
            await client.SendTextMessageAsync(message.Chat.Id, "Успешно.");
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterPhotoTable;
        }
    }
}