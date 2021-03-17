using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class TopUpCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await client.SendTextMessageAsync(user.Id,
                "Введите сумму, на которую хотите пополнить баланс.");
            user.state = State.enterTopUp;
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("🎲Пополнить счет") && user.room is null;
        }
    }
}