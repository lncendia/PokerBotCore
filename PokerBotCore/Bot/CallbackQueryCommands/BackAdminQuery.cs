using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class BackAdminQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            await client.SendTextMessageAsync(query.From.Id, "Вы в гланом меню.", replyMarkup:MainKeyboards.AdminKeyboard);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return user.state == State.admin;
        }
    }
}