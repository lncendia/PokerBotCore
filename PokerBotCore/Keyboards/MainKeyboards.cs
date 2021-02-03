using System.Collections.Generic;
using System.Linq;
using PokerBotCore.Entities;
using PokerBotCore.Rooms;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Keyboards
{
    public static class MainKeyboards
    {
        public static readonly ReplyKeyboardMarkup MainKeyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>()
        {
            new List<KeyboardButton>() {new KeyboardButton("🃏Список комнат"), new KeyboardButton("🥊Создать комнату")},
            new List<KeyboardButton>() {new KeyboardButton("🎲Пополнить счет"), new KeyboardButton("💸Вывод")},
            new List<KeyboardButton>() {new KeyboardButton("👤Профиль"), new KeyboardButton("⁉️Оставить отзыв")},
            new List<KeyboardButton>() {new KeyboardButton("📬Игровой чат")}
        });

        public static readonly ReplyKeyboardMarkup AdminKeyboard = new ReplyKeyboardMarkup(
            new List<List<KeyboardButton>>()
            {
                new List<KeyboardButton>() {new KeyboardButton("Рассылка"), new KeyboardButton("Комнаты с ботами")},
                new List<KeyboardButton>()
                    {new KeyboardButton("Добавить средства"), new KeyboardButton("Просмотр отзывов")},
                new List<KeyboardButton>() {new KeyboardButton("/admin")}
            });

        public static readonly InlineKeyboardMarkup CreateRoomKeyboard = new InlineKeyboardMarkup(
            new List<List<InlineKeyboardButton>>()
            {
                {
                    new List<InlineKeyboardButton>()
                        {InlineKeyboardButton.WithCallbackData("Поделиться в чате", "sentroom")}
                },
                new List<InlineKeyboardButton>() {InlineKeyboardButton.WithCallbackData("Отмена", "exit")}
            });

        public static readonly InlineKeyboardMarkup CreatePrivateRoomKeyboard =
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Отмена", "exit"));
        
        public static InlineKeyboardMarkup CreateConnectButton(Room room)
        {
            return InlineKeyboardButton.WithCallbackData(room.key != 0 ? $"🔒Комната {room.id} [{room.players.Count}/{room.countPlayers}]" : $"Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString());
        }
        public static InlineKeyboardMarkup CreateConnectButton(List<Room> rooms)
        {
            var key = new List<List<InlineKeyboardButton>>();
            foreach (var room in rooms.TakeWhile(room => key.Count != 50).Where(room => (room.players.Count != 0 && room.players[0].state == User.State.wait)))
            {
                key.Add(room.key != 0
                    ? new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData(
                            $"🔒Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString())
                    }
                    : new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData(
                            $"Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString())
                    });
            }

            return new InlineKeyboardMarkup(key);
        }
    }
}