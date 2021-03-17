using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using PokerBotCore.Rooms;
using PokerBotCore.Rooms.RoomTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class MainCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!int.TryParse(message.Text, out int id))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                return;  
            }

            Room room = Operations.GetRoom(id);
            if (room == null || room.started)
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Комната не существует или игра в ней уже началась.");
                return;
            }

            await client.SendTextMessageAsync(message.Chat.Id, "Список комнат:",
                replyMarkup: MainKeyboards.CreateConnectButton(room));
        }

        public bool Compare(Message message, User user)
        {
            return false;//user.state == State.main;
        }
    }
}