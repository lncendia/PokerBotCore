using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Bot;
using PokerBotCore.Enums;
using PokerBotCore.Keyboards;
using PokerBotCore.Model;
using PokerBotCore.Rooms.RoomTypes;

namespace PokerBotCore.Rooms
{
    public static class ExceptionHandler
    {
        public static async Task ExceptionInGame(Exception ex, Room room, bool isFinal = false)
        {
            BotSettings.reviews.Enqueue(
                $"0:Эксепшн: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
            if (room.block && !isFinal) return;
            room.block = true;
            EmergencySaveMoney(room.players, room);
            await room.SendMessage($"Произошла ошибка. Приносим свои извинения, средства были возвращены!", room.players,
                MainKeyboards.MainKeyboard, needLeave:false);
            foreach (User user in room.players.ToList())
            {
                room.RemovePlayer(user);
            }
        }

        private static void EmergencySaveMoney(List<User> players, Room room)
        {
            room.bet = 0;
            foreach (User user in players.ToList())
            {
                user.state = State.main;
                user.Money += user.bet;
            }
        }
    }
}