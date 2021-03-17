using System;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using PokerBotCore.Rooms;
using PokerBotCore.Rooms.RoomTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class CreatePrivateRoomQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            int count = Int32.Parse(query.Data.Substring(8));
            Room room = Operations.CreateRoom(count, user, true);
            user.firstName = query.From.FirstName;
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            await client.SendTextMessageAsync(query.From.Id,
                $"Создана комната с ID {room.id}. Ожидаем подключения других игроков.",
                replyMarkup: MainKeyboards.CreatePrivateRoomKeyboard);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.Contains("private") && user.state == State.enterCountPlayers;
        }
    }
}