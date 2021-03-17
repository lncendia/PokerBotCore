using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using PokerBotCore.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class RemoveFriendCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await using Db db = new Db();
            long id = long.Parse(message.Text.Split('_')[1]);
            Friendship friendship = BotSettings.friendships.FirstOrDefault(x => x.Id == id);
            if (friendship == null) return;
            if (friendship.User1 == user.Id || friendship.User2 == user.Id)
            {
                BotSettings.friendships.Remove(friendship);
                db.Friendships.Remove(friendship);
                await db.SaveChangesAsync();
            }

            await client.SendTextMessageAsync(user.Id, $"Друг удален.");
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("remove_");
        }
    }
}