using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterPasswordCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!int.TryParse(message.Text, out int keyRoom))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                return;  
            }

            var room = Operations.GetRoom(user.idPrivateRoom);
            if (room == null)
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Комната не доступна для подключения. Возможно игра в ней уже началась.");
                user.idPrivateRoom = 0;
                return;
            }

            if (room.key != 0 && room.key == keyRoom)
            {
                room.AddPlayer(user, message.Chat.FirstName);
                user.idPrivateRoom = 0;
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Пароль неверный.");
            }

        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterPassword;
        }
    }
}