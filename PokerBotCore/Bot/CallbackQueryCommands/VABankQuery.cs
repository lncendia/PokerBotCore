using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class VABankQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.Money < user.room.lastRaise - user.lastRaise)
            {
                await using Db db = new Db();
                user.room.allInUsers.Add(user);
                user.room.bet += user.Money;
                user.bet += user.Money;
                user.lastRaise += user.Money;
                user.Money = 0;
                user.room.next = true;
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Ход переходит к следующему игроку.");
                db.UpdateRange(user);
                await db.SaveChangesAsync();
                user.room.SendMessage($"Игрок {user.firstName} пошел ва-банк.", user.room.players,
                    null);
            }

            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "VA-Bank" && user.state == State.waitBet;
        }
    }
}