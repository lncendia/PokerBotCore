using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using PokerBotCore.Payments;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class CheckPaymentQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (!Transactions.CheckPay(user, query.Data.Substring(5))) return;
            string message = query.Message.Text;
            message = message.Replace("Не оплачено", "Оплачено");
            await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                message);

        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.StartsWith("bill");
        }
    }
}