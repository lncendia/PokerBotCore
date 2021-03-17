using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterIdFakeRoomToDeleteCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (int.TryParse(message.Text, out int count))
            {
                var fakeRoom = Operations.GetFaceRoom(count);
                if(fakeRoom==null||fakeRoom.needDelete) return;
                fakeRoom.needDelete = true;
                await client.SendTextMessageAsync(user.Id,
                    "Комната будет удалена по окончанию игры.");
                user.state = State.admin;
            }
            else
            {
                await client.SendTextMessageAsync(user.Id,
                    "Введи число.");
            }
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterIdFakeRoomToDelete;
        }
    }
}