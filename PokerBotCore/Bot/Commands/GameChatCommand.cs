using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class GameChatCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await client.SendTextMessageAsync(user.Id,
                "Доступ в игровой чат можно получить по <a href=\"https://t.me/joinchat/3eUCfmomV9MyNzVi\">ссылке</a>.",
                ParseMode.Html);
        }

        public bool Compare(Message message, User user)
        {
            return message.Text == "📬Игровой чат" && user.room is null;
        }
    }
}