using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class CallQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await using Db db = new Db();
            var x = user.room.lastRaise - user.lastRaise;
            if (user.Money >= x)
            {
                user.Money -= x;
                user.room.bet += x;
                user.lastRaise += x;
                user.bet += x;
                if (user.Money == 0) user.room.allInUsers.Add(user);
                user.room.next = true;
                db.UpdateRange(user);
                await db.SaveChangesAsync();
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Ход переходит к следующему игроку.");
            }
            else
            {
                await client.SendTextMessageAsync(query.From.Id, $"Недостаточно средств!",
                    replyMarkup: GameKeyboards.VaBank);
            }

            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "Call" && user.state == State.waitBet;
        }
    }
}