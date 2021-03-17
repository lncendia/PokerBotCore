using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using PokerBotCore.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class AddFriendCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            User info;
            await using Db db = new Db();
            try
            {
                string information = message.Text.Split(' ')[1];
                info = BotSettings.users.FirstOrDefault(
                    x => x.Id == long.Parse(information.Substring(information.IndexOf('_') + 1)));
                if (info == null) return;
                if (info.Id == message.Chat.Id) return;
            }
            catch
            {
                return;
            }

            var f = BotSettings.friendships.ToList().FirstOrDefault(friendship =>
                (friendship.User1 == user.Id && friendship.User2 == info.Id) ||
                (friendship.User1 == info.Id && friendship.User2 == user.Id));
            if (f != null) return;
            db.UpdateRange(user, info);
            Friendship friend = new Friendship {User1 = user.Id, User2 = info.Id, Accepted = false};
            BotSettings.friendships.Add(friend);
            await db.Friendships.AddAsync(friend);
            await db.SaveChangesAsync();
            await client.SendTextMessageAsync(user.Id, $"Пользователю отправлена заявка в друзья.");
            await client.SendTextMessageAsync(info.Id,
                $"Пользователь @{message.From.Username} отправил вам заявку в друзья.",
                replyMarkup: MainKeyboards.FriendsRequest(user.Id));
            
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("info_");
        }
    }
}