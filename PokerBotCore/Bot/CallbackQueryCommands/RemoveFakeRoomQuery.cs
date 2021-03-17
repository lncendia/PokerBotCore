using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class RemoveFakeRoomQuery: ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await client.SendTextMessageAsync(query.From.Id, "Введите id комнаты.",replyMarkup:MainKeyboards.BackAdmin);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return user.state == State.enterIdFakeRoomToDelete;
        }
    }
}