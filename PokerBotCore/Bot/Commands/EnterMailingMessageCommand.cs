using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterMailingMessageCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            foreach (User user1 in BotSettings.users)
            {
                await client.SendTextMessageAsync(user1.Id, message.Text);
            }
            user.state = State.admin;
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterMailingMessage;
        }
    }
}