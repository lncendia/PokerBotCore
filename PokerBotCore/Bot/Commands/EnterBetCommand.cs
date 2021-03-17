using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterBetCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!int.TryParse(message.Text, out int raise))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                return;  
            }
            if (raise > 1000)
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Ставка не может быть больше 1000 коинов.");
                return;
            }

            if (raise > user.Money)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Недостаточно средств!");
                return;
            }

            if (raise < 25) /////////////////////////////////////////////////
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Ставка должна быть больше 25 коинов.");
                return;
            }

            await using Db db = new Db();
            int raise1 = user.room.lastRaise - user.lastRaise + raise;
            user.bet += raise1;
            user.room.lastRaise += raise;
            user.room.bet += raise1;
            user.Money -= raise1;
            user.lastRaise += raise1;
            if (user.Money == 0) user.room.allInUsers.Add(user);
            user.room.next = true;
            db.UpdateRange(user);
            await db.SaveChangesAsync();
            user.room.SendMessage($"Игрок {user.firstName} повысил ставку на {raise} коинов.",
                user.room.players, null);
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.waitBet;
        }
    }
}