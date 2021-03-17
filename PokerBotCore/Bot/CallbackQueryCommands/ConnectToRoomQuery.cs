using System;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class ConnectToRoomQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (user.Money < 40) ////////////////////////////////////////////////////////////
            {
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Недостаточно средств. Счет должен быть больше 40 коинов.");
                await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
                return;
            }

            if (!int.TryParse(query.Data, out int idRoom)) return;
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            var room = Operations.GetRoom(idRoom);
            if (room != null && !room.started)
            {
                if (room.key != 0)
                {
                    user.idPrivateRoom = idRoom;
                    user.state = State.enterPassword;
                    await client.SendTextMessageAsync(query.From.Id, $"Введите пароль.");
                }
                else
                {
                    room.AddPlayer(user, query.From.FirstName);
                }
            }
            else
            {
                await client.AnswerCallbackQueryAsync(query.Id,
                    "Комната не доступна для подключения. Возможно игра в ней уже началась.");
            }
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return user.state == State.main;
        }
    }
}