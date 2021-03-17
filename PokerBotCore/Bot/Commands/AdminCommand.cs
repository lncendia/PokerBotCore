using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using PokerBotCore.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class AdminCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (user.state == State.admin)
            {
                await client.SendTextMessageAsync(user.Id, "Вы вышли из меню админа.",
                    replyMarkup: MainKeyboards.MainKeyboard);
                user.state = State.main;
            }
            else
            {
                user.state = State.admin;
                await MainBot.Tgbot.SendTextMessageAsync(user.Id,
                    "Добро пожаловать в админ-панель.",
                    replyMarkup: MainKeyboards.AdminKeyboard);
            }

        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("/admin") && message.From.Id == 346978522 && user.room is null;
        }
    }
}