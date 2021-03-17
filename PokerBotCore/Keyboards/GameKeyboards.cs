using System.Collections.Generic;
using PokerBotCore.Model;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Keyboards
{
    public static class GameKeyboards
    {
        public static readonly InlineKeyboardMarkup VaBank =
            new(InlineKeyboardButton.WithCallbackData("Ва-банк", "VA-Bank"));
        public static readonly InlineKeyboardMarkup DoKeyboard = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("Fold", "Fold"),
                InlineKeyboardButton.WithCallbackData("Check", "Check"),
                InlineKeyboardButton.WithCallbackData("Raise", "Raise")
            });

        public static InlineKeyboardMarkup DoKeyboardCall(int count)
        {
            return new(
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("Fold", "Fold"),
                    InlineKeyboardButton.WithCallbackData($"Call {count}", "Call"),
                    InlineKeyboardButton.WithCallbackData("Raise", "Raise")
                });
        }

        public static InlineKeyboardMarkup CombinationKeyboard(User user1)
        {
            return new(new List<List<InlineKeyboardButton>>()
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData(user1.cards[0]),
                    InlineKeyboardButton.WithCallbackData(user1.cards[1])
                },
                new() {InlineKeyboardButton.WithCallbackData(user1.combination.ToString())}
            });
        }
        public static readonly ReplyKeyboardMarkup Exit = new(new KeyboardButton("Выход"));
    }
}