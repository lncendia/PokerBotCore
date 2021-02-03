using System.Collections.Generic;
using PokerBotCore.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Keyboards
{
    public static class GameKeyboards
    {
        public static readonly InlineKeyboardMarkup VaBank =
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ва-банк", "VA-Bank"));
        public static readonly InlineKeyboardMarkup DoKeyboard = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("Fold", "Fold"),
                InlineKeyboardButton.WithCallbackData("Check", "Check"),
                InlineKeyboardButton.WithCallbackData("Raise", "Raise")
            });
        public static InlineKeyboardMarkup CombinationKeyboard(User user1, string combination)
        {
            return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData(user1.cards[0]),
                    InlineKeyboardButton.WithCallbackData(user1.cards[1])
                },
                new List<InlineKeyboardButton>() {InlineKeyboardButton.WithCallbackData(combination)}
            });
        }
        public static readonly ReplyKeyboardMarkup Exit = new ReplyKeyboardMarkup(new KeyboardButton("Выход"));
    }
}