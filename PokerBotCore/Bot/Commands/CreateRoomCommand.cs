using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.Commands
{
    public class CreateRoomCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            if (user.Money < 40) //////////////////////////////
            {
                await client.SendTextMessageAsync(message.Chat.Id,
                    "Недостаточно средств. Счет должен быть больше 40 коинов.");
                return;
            }

            user.state = State.enterCountPlayers;
            await client.SendTextMessageAsync(message.Chat.Id, "Введите количество мест. От 2 до 5.");
        }

        public bool Compare(Message message, User user)
        {
            return message.Text.Contains("🥊Создать комнату") && user.room is null;
        }
    }
}