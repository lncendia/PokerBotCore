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
    public class ProfileCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            user.state = State.main;
            string str =
                $"Ваш ID: {user.Id}\nВаши средства: {user.Money}\nВаша реферальная ссылка: https://t.me/PokerGame777_bot?start={message.From.Id}";
            if (user.Referal != null)
            {
                str += $"\nВас пригласил: {user.Referal.Id}";
            }

            await client.SendTextMessageAsync(message.Chat.Id, str,
                replyMarkup: MainKeyboards.ProfileKeyboard);
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("👤Профиль") && user.room is null;
        }
    }
}