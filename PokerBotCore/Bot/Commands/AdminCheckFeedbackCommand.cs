using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class AdminCheckFeedbackCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            while (BotSettings.reviews.IsEmpty)
            {
                var x = BotSettings.reviews.TryDequeue(out string review);
                if (!x) break;
                var split = review.Split(new[] {':'}, 2);
                var id = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ответить", split[0]));
                await MainBot.Tgbot.SendTextMessageAsync(user.Id, split[1], replyMarkup: id);
            }
        }

        public bool Compare(Message message, User user)
        {
            return message.Text=="Просмотр отзывов" && user.state == State.admin;
        }
    }
}