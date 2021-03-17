using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterCoinCountCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await using Db db = new Db();
            try
            {
                var x = message.Text.Split(':');
                User user1 = BotSettings.users.FirstOrDefault(y => y.Id == int.Parse(x[0]));
                if (user1 != null)
                {
                    user1.AddMoney(int.Parse(x[1]));
                    db.Update(user1);
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await MainBot.Tgbot.SendTextMessageAsync(message.Chat.Id, $"Ошибка: {ex.Message}");
            }

            user.state = State.admin;
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterCoinCount;
        }
    }
}