using System;
using System.Linq;
using PokerBotCore.Enums;
using PokerBotCore.Keyboards;
using PokerBotCore.Model;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Bot
{
    public static class Admin
    {
        public static async void Tgbot_AdminCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            if(e.CallbackQuery.From.Id!=346978522) return;
            var user = Operations.GetUser(e.CallbackQuery.From.Id);
            if (user == null || user.room != null) return;
            switch (e.CallbackQuery.Data)
            {
                case "createFakeRoom":
                    user.state = State.enterCountPlayersOfFakeRoom;
                    await MainBot.Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Введите количество игроков.",replyMarkup:MainKeyboards.BackAdmin);
                    break;
                case "removeFakeRoom":
                    user.state = State.enterIdFakeRoomToDelete;
                    await MainBot.Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Введите id комнаты.",replyMarkup:MainKeyboards.BackAdmin);
                    break;
                case "backAdmin":
                    user.state = State.admin;
                    await MainBot.Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    await MainBot.Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Вы в гланом меню.", replyMarkup:MainKeyboards.AdminKeyboard);
                    break;
                default:
                    if (user.state == State.admin)
                    {
                        user.state = State.enterAnswerMessage;
                        await MainBot.Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Введите сообщение.");
                        user.idForAnswer = int.Parse(e.CallbackQuery.Data);
                    }
                    break;
            }
        }
    }
}