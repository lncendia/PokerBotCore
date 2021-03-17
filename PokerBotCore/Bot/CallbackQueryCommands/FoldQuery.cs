using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class FoldQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            user.combination = null;
            user.room.foldUsers.Add(user);
            user.lastRaise = 0;
            if (user.state == State.waitBet) user.room.next = true;
            await client.AnswerCallbackQueryAsync(query.Id,
                "Ход переходит к следующему игроку.");
            user.room.SendMessage($"Игрок {user.firstName} сбросил карты.", user.room.players, null);
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "Fold" && user.state == State.waitBet;
        }
    }
}