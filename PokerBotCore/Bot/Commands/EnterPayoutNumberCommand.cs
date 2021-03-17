using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Payments;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterPayoutNumberCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!message.Text.Contains("+"))
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Номер должен начинаться с \"+\"");
                return;
            }

            bool success = Transactions.OutputTransaction(message.Text, user);
            user.output = 0;
            user.state = State.main;
            if (success)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Запрос отправлен.");
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Произошла ошибка. Попробуйте позже.");
            }
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterPayoutNumber;
        }
    }
}