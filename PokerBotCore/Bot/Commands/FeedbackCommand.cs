using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class FeedbackCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await client.SendTextMessageAsync(message.Chat.Id,
                "Напишите отзыв. Он будет отпрален автору бота.");
            user.state = State.enterFeedback;
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("⁉Оставить отзыв") && user.room is null;
        }
    }
}