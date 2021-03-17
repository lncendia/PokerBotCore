using System;
using System.Collections.Generic;
using System.Linq;
using PokerBotCore.Bot;
using PokerBotCore.Rooms;
using PokerBotCore.Rooms.RoomTypes;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Keyboards
{
    public static class MainKeyboards
    {
        public static readonly ReplyKeyboardMarkup MainKeyboard = new(new List<List<KeyboardButton>>()
        {
            new() {new KeyboardButton("🃏Список комнат"), new KeyboardButton("🥊Создать комнату")},
            new() {new KeyboardButton("🎲Пополнить счет"), new KeyboardButton("💸Вывод")},
            new() {new KeyboardButton("👤Профиль"), new KeyboardButton("⁉Оставить отзыв")},
            new() {new KeyboardButton("📬Игровой чат")}
        });

        public static readonly ReplyKeyboardMarkup AdminKeyboard = new(
            new List<List<KeyboardButton>>()
            {
                new() {new KeyboardButton("Рассылка"), new KeyboardButton("Комнаты с ботами")},
                new()
                    {new KeyboardButton("Добавить средства"), new KeyboardButton("Просмотр отзывов")},
                new() {new KeyboardButton("/admin")}
            });

        public static readonly InlineKeyboardMarkup BackAdmin = new(
            InlineKeyboardButton.WithCallbackData("В главное меню", "backAdmin"));
        public static readonly InlineKeyboardMarkup CreateOrRemoveFaceRoom = new(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Добавить", "createFakeRoom"),
            InlineKeyboardButton.WithCallbackData("Удалить", "removeFakeRoom")
        });

        public static readonly InlineKeyboardMarkup CreateRoomKeyboard = new(
            new List<List<InlineKeyboardButton>>()
            {
                {
                    new()
                        {InlineKeyboardButton.WithCallbackData("Поделиться в чате", "sentroom")}
                },
                new() {InlineKeyboardButton.WithCallbackData("Отмена", "exit")}
            });

        public static readonly InlineKeyboardMarkup CreatePrivateRoomKeyboard =
            new(InlineKeyboardButton.WithCallbackData("Отмена", "exit"));

        public static readonly InlineKeyboardMarkup AreYouSure =
            new(InlineKeyboardButton.WithCallbackData("Да", "exit"));

        public static readonly InlineKeyboardMarkup StandartTable =
            new(InlineKeyboardButton.WithCallbackData("Установить стандартный фон", "standard_table"));

        public static readonly InlineKeyboardMarkup ProfileKeyboard = new(new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData("Друзья", "friends"),
            InlineKeyboardButton.WithCallbackData("Сменить фон стола", "change_table")
        });

        public static readonly InlineKeyboardMarkup SentedRequest =
            new(InlineKeyboardButton.WithCallbackData("Отправленные заявки", "sentedRequest"));
        public static InlineKeyboardMarkup FriendsRequest(long id)
        {
            return new(new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("Добавить", $"Add_{id}"),
            });
        }

        public static InlineKeyboardMarkup CheckBill(string billId)
        {
            return new(
                InlineKeyboardButton.WithCallbackData("Проверить оплату", $"bill_{billId}"));
        }

        public static InlineKeyboardMarkup CreateConnectButton(Room room)
        {
            return InlineKeyboardButton.WithCallbackData(
                room.key != 0
                    ? $"🔒Комната {room.id} [{room.players.Count}/{room.countPlayers}]"
                    : $"Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString());
        }

        public static InlineKeyboardMarkup CreateConnectButton()
        {
            List<Room> rooms = BotSettings.rooms;
            var key = new List<List<InlineKeyboardButton>>();
            foreach (var room in rooms.TakeWhile(_ => key.Count != 50).Where(room =>
                room.players.Count != 0 && !room.started))
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

        public static InlineKeyboardMarkup CreateRoomSelect(int count)
        {
            return new(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Публичная", $"public_{count}"),
                InlineKeyboardButton.WithCallbackData("Приватная", $"private_{count}")
            });
        }
    }
}