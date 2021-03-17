using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Interfaces
{
    public interface ICallbackQueryCommand
    {
        public Task Execute(TelegramBotClient client, User user, CallbackQuery query);
        public bool Compare(CallbackQuery query, User user);
    }
}