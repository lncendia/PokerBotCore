using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class RaiseQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await client.SendTextMessageAsync(query.From.Id,
                $"Введите колличество. На вашем счету {user.Money} коинов. Максимальная ставка: 1000 коинов.");
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "Raise" && user.state == State.waitBet;
        }
    }
}