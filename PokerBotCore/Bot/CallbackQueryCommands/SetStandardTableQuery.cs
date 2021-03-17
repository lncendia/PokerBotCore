using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class SetStandardTableQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.state == State.enterPhotoTable || user.state == State.main)
            {
                await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
                File.Delete($"tables\\{user.Id}.jpg");
                await client.AnswerCallbackQueryAsync(query.Id, "Фон изменен на стандартный.");
                user.state = State.main;
            }
            else
            {
                await client.AnswerCallbackQueryAsync(query.Id, "Невозможно изменить фон сейчас.");
            }
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "standard_table";
        }
    }
}