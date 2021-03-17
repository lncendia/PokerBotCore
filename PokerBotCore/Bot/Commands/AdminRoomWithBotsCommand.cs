using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class AdminRoomWithBotsCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            string str = "Фейк комнаты: ";
            foreach (var room in BotSettings.fakeRooms.ToList())
            {
                string start = room.started
                    ? "Игра идет."
                    : "Ожидание.";
                str += $"Комната {room.id}. [{room.players.Count}/{room.countPlayers}]. " + start+"\n";
            }
            await client.SendTextMessageAsync(user.Id, str,replyMarkup:MainKeyboards.CreateOrRemoveFaceRoom);
        }

        public bool Compare(Message message, User user)
        {
            return message.Text == "Комнаты с ботами" && user.state == State.admin;
        }
    }
}