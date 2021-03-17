using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class PayoutCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            user.state = State.enterPayout;
            await client.SendTextMessageAsync(message.Chat.Id, "Введите сумму, которую хотите вывести.");
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("💸Вывод") && user.room is null;
        }
    }
}