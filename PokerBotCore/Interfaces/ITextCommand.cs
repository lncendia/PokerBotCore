using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using PokerBotCore.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Interfaces
{
    public interface ITextCommand
    {
        public Task Execute(TelegramBotClient client, User user, Message message);
        public bool Compare(Message message, User user);
    }
}