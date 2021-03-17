using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class AdminMailingCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await client.SendTextMessageAsync(user.Id, "Введите сообщение.",replyMarkup:MainKeyboards.BackAdmin);
            user.state = State.enterMailingMessage;
        }

        public bool Compare(Message message, User user)
        {
            return message.Text == "Рассылка" && user.state == State.admin;
        }
    }
}