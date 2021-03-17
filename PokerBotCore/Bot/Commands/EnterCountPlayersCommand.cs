using System;
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
    public class EnterCountPlayersCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (!int.TryParse(message.Text, out int count))
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                return;  
            }

            if (count < 2 || count > 5)
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Написано же! От 2 до 5.");
                return;
            }

            await client.SendTextMessageAsync(message.Chat.Id, "Выберете тип комнаты:",
                replyMarkup: MainKeyboards.CreateRoomSelect(count));
        }

        public bool Compare(Message message, User user)
        {
            return user.state == State.enterCountPlayers;
        }
    }
}