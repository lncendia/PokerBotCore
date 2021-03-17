using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class EnterAnswerMessageCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            try
            {
                await MainBot.Tgbot.SendTextMessageAsync(user.idForAnswer,
                    $"Администратор @{message.From.FirstName} ответил вам: {message.Text}");
                await client.SendTextMessageAsync(user.Id, "Сообщение отправлено!");
            }
            catch (Exception ex)
            {
                await client.SendTextMessageAsync(user.Id,
                    $"Ошибка: {ex.Message} Сообщение не отправлено!");
            }

            user.state = State.admin;
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterAnswerMessage;
        }
    }
}