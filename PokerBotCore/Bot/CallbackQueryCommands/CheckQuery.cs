using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class CheckQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.room.lastRaise == 0 || user.room.lastRaise - user.lastRaise == 0)
            {
                user.room.next = true;
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Ход переходит к следующему игроку.");
            }
            else
            {
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Отличная попытка схитрить... Но нет.", true);
                return;
            }

            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "Check" && user.state == State.waitBet;
        }
    }
}