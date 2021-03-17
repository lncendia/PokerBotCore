using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using PokerBotCore.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class CheckFriendsQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            var f = BotSettings.friendships.ToList().Where(friendship =>
                (friendship.User1 == user.Id || friendship.User2 == user.Id) && friendship.Accepted).ToList();
            if (!f.Any())
            {
                await client.SendTextMessageAsync(query.From.Id, "У вас нет друзей :(",
                    replyMarkup: MainKeyboards.SentedRequest);
                return;
            }

            string friends = "Ваши друзья:\n";
            foreach (Friendship friend in f)
            {
                int id = friend.User1 == user.Id ? (int) friend.User2 : (int) friend.User1;
                var user2 = await client.GetChatMemberAsync(id, id);
                var friendUser = BotSettings.users.FirstOrDefault(x => x.Id == friend.User2);
                string online = friendUser != null && friendUser.countMessages > 0
                    ? "В сети"
                    : "Не в сети";
                if (friendUser?.room != null && friendUser.state == State.wait)
                {
                    friends +=
                        $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.Id}\">-</a>)@{user2.User.Username} (<a href =\"https://telegram.me/PokerGame777_bot?start=connect_{friendUser.room.id}\">В игре</a>)\n";
                }
                else
                    friends +=
                        $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.Id}\">-</a>)@{user2.User.Username} ({online})\n";
            }

            await client.SendTextMessageAsync(query.From.Id, friends,
                replyMarkup: MainKeyboards.SentedRequest,
                parseMode: ParseMode.Html);

        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "friends" && user.state == State.main;
        }
    }
}