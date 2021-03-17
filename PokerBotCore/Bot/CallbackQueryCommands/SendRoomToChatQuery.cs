using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class SendRoomToChatQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.state != State.wait)
            {
                await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
                return;
            }

            await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                query.Message.Text, replyMarkup: MainKeyboards.CreatePrivateRoomKeyboard);
           // SendMessageToChat($"Приглашаю вас в комнату {user.room.id}.", query.From.Username,
              //  user, MainKeyboards.CreateConnectButton(user.room));
            await client.AnswerCallbackQueryAsync(query.Id, $"Приглашение отправлено.");
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data == "sentroom";
        }
    }
}