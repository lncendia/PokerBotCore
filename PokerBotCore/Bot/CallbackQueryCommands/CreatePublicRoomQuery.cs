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
    public class CreatePublicRoomQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            int count = Int32.Parse(query.Data.Substring(7));
            Room room = Operations.CreateRoom(count, user, false);
            user.firstName = query.From.FirstName;
            await client.DeleteMessageAsync(query.Message.Chat.Id, query.Message.MessageId);
            await client.SendTextMessageAsync(query.From.Id,
                $"Создана комната с ID {room.id}. Ожидаем подключения других игроков.",
                replyMarkup: MainKeyboards.CreateRoomKeyboard);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.Contains("public") && user.state == State.enterCountPlayers;
        }
    }
}