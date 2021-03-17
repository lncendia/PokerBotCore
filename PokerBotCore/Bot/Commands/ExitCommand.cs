using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class ExitCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await client.SendTextMessageAsync(message.Chat.Id, "Вы уверены?",
                    replyMarkup: MainKeyboards.AreYouSure);
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("Выход") && user.room != null;
        }
    }
}