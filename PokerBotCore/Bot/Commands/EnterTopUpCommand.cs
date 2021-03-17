using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using PokerBotCore.Payments;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterTopUpCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!int.TryParse(message.Text, out int raise))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                return;  
            }

            switch (raise)
            {
                case > 99999:
                    await client.SendTextMessageAsync(message.Chat.Id, "Слишком большая сумма!");
                    return;
                case < 30:
                    await client.SendTextMessageAsync(message.Chat.Id,
                        "Сумма должна быть больше 30 рублей!");
                    return;
            }

            var billId = "";
            var payUrl = Transactions.NewTransaction(raise, user, ref billId);
            if (payUrl == null)
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Произошла ошибка. Попробуйте еще раз.");
                return;
            }

            await client.SendTextMessageAsync(message.Chat.Id,
                $"Пополнение счета на сумму {raise} р.\nДата: {DateTime.Now:dd.MMM.yyyy}\nСтатус: Не оплачено.\n\nОплатите счет по ссылке.\n{payUrl}",
                replyMarkup: MainKeyboards.CheckBill(billId));
            user.state = State.main;
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterTopUp;
        }
    }
}