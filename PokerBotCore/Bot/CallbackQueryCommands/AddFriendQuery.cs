using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class AddFriendQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (!long.TryParse(query.Data[4..], out long id)) return;
            User info = BotSettings.users.FirstOrDefault(x => x.Id == id);
            if (info == null)
            {
                await client.AnswerCallbackQueryAsync(query.Id, $"Пользователь не найден.");
                return;
            }

            var f = BotSettings.friendships.ToList().FirstOrDefault(friendship =>
                (friendship.User1 == user.Id && friendship.User2 == info.Id) ||
                (friendship.User1 == info.Id && friendship.User2 == user.Id));
            if (f == null)
            {
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Заявка была отклонена пользователем.");
                return;
            }

            if (f.Accepted)
            {
                await client.AnswerCallbackQueryAsync(query.Id, "Вы уже друзья.");
                return;
            }

            await using Db db = new Db();
            db.UpdateRange(user, f);
            f.Accepted = true;
            await db.SaveChangesAsync();
            await client.AnswerCallbackQueryAsync(query.Id, $"Запрос принят.");
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.StartsWith("Add");
        }
    }
}