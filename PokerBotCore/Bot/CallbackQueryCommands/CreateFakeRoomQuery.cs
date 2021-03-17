using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class CreateFakeRoomQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await MainBot.Tgbot.SendTextMessageAsync(query.From.Id, "Введите количество игроков.",
                replyMarkup: MainKeyboards.BackAdmin);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return user.state == State.enterCountPlayersOfFakeRoom;
        }
    }
}