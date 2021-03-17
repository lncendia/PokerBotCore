using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class ListRoomsCommand : ITextCommand
    {

        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            user.state = State.main;
            var key = MainKeyboards.CreateConnectButton();
            if (key.InlineKeyboard.Count() != 0)
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Показаны первые 50 комнат. Вы можете ввести ID нужной вам комнаты.",
                    replyMarkup: key);
            else if (client != null) await client.SendTextMessageAsync(message.Chat.Id, "Комнаты не найдены.");
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("🃏Список комнат") && user.room is null;
        }
    }
}