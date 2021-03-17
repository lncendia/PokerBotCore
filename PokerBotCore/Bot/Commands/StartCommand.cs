using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class StartCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await using Db db = new Db();
            var refer = long.TryParse(message.Text.Split(' ')[1], out long id) ? BotSettings.users.FirstOrDefault(x => x.Id == id) : null;
            if (refer != null && refer.Id == message.Chat.Id) refer = null;
            if (refer != null)
            {
                try
                {
                    await client.SendTextMessageAsync(refer.Id,
                        $"По вашей реферальной ссылке подключился игрок @{message.From.Username}.");
                }
                catch
                {
                    // ignored
                }
            }

            var userToAdd = new User() {Id = message.From.Id, Money = 0, Referal = refer};
            BotSettings.users.Add(userToAdd);
            if (refer != null) db.Update(refer);
            await db.AddAsync(userToAdd);
            await db.SaveChangesAsync();
            await client.SendTextMessageAsync(message.Chat.Id,
                $"Добро пожаловать! Пополни свой счет и вперед играть!\nТвоя реферальная ссылка: https://t.me/PokerGame777_bot?start={message.From.Id} \nЗа каждого приглашенного игрока вы будете получать 7% от его пополнений.",
                replyMarkup: MainKeyboards.MainKeyboard);
            return;
        }

        public bool Compare(Message message, User user)
        {
            return user is null;
        }
    }
}