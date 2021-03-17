using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class CreatePrivateRoomQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            throw new System.NotImplementedException();
        }

        public bool Compare(string command, User user)
        {
            throw new System.NotImplementedException();
        }
    }
}