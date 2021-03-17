using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class AnswerAdminQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.state == State.admin)
            {
                user.state = State.enterAnswerMessage;
                await client.AnswerCallbackQueryAsync(query.Id, "Введите сообщение.");
                user.idForAnswer = int.Parse(query.Data);
            }
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return user.state == State.admin;
        }
    }
}