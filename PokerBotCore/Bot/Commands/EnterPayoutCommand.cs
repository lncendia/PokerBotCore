using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterPayoutCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!int.TryParse(message.Text, out int money))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                return;  
            }

            if (money > user.Money)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Недостаточно средств.");
                return;
            }

            if (money < 50) //TODO: Ограничение на вывод.
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Вывод средств осуществляется от 50 рублей.");
                return;
            }

            user.output = money;
            await client.SendTextMessageAsync(message.Chat.Id,
                "Введите номер QIWI кошелька, на который будет осуществляться вывод.\nФормат: +<код страны><номер>");
            user.state = State.enterPayoutNumber;
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterPayout;
        }
    }
}