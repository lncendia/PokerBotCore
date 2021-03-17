using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterFeedbackCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            user.state = State.main;
            BotSettings.reviews.Enqueue($"{user.Id}:{message.Chat.FirstName} ({user.Id}): {message.Text}");
            await client.SendTextMessageAsync(message.Chat.Id,
                "Спасибо за отзыв. Мы рассмотрим его в ближайшее время.");
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterFeedback;
        }
    }
}