﻿using System.Threading.Tasks;
using PokerBotCore.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot.CallbackQueryCommands
{
    public class ExitRoomQuery : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
            if (user.room != null)
            {
                while (user.room.block)
                {
                }

                user.room.RemovePlayer(user);
            }

            await client.AnswerCallbackQueryAsync(query.Id, $"Вы покинули комнату.");
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.Contains("exit") && user.room != null;
        }
    }
}