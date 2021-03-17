using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using PokerBotCore.Rooms;
using PokerBotCore.Rooms.RoomTypes;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class ConnectToFriendCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (message.Text.Contains("connect_"))
            {
                int idRoom = Int32.Parse(message.Text.Split('_')[1]);
                Room room = BotSettings.rooms.Find((room1 => room1.id == idRoom));
                if (room == null) return;
                await client.SendTextMessageAsync(user.Id, $"Комната вашего друга:",
                    replyMarkup: MainKeyboards.CreateConnectButton(room));
            }
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("connect_");
        }
    }
}