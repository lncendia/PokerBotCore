using System;
using System.Linq;
using System.Net.Http;
using RestSharp;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;
using User = PokerBotCore.Model.User;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PokerBotCore.Bot
{
    internal static class MainBot
    {
        public static readonly TelegramBotClient Tgbot = BotSettings.Get();

        public static void Start()
        {
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Tgbot.OnUpdate+= TgbotOnOnUpdate;
            Operations.Mute();
            Tgbot.StartReceiving();
        }

        private static void TgbotOnOnUpdate(object sender, UpdateEventArgs e)
        {
            var client = new RestClient("https://localhost:5001/Cars/List");
            var request = new RestRequest(Method.POST); 
            request.AlwaysMultipartFormData = true;
            request.AddParameter("d", "f");
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);
            File.WriteAllText("js.json",JsonSerializer.Serialize(e.Update, e.Update.GetType()));
        }

        private static async void Tgbot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            // try
            // {
                User user = BotSettings.users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;
                var command = BotSettings.callbackQueryCommands.FirstOrDefault(_ => _.Compare(e.CallbackQuery, user));
                if (command != null) await command.Execute(Tgbot, user, e.CallbackQuery);
            //}
            // catch (Exception ex)
            // {
            //     BotSettings.reviews.Enqueue(
            //         $"Ошибка у пользователя {e.CallbackQuery.From.Id}: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
            // }
        }
        
        private static async void Tgbot_OnMessage(object sender, MessageEventArgs e)
        {
            // try
            // {
            var message = e.Message;
                if (message.Type != MessageType.Text && message.Type != MessageType.Photo) return;
                var user = Operations.GetUser(message.From.Id);
                if (user != null)
                {
                    user.countMessages++;
                    switch (user.countMessages)
                    {
                        case 10:
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, не флудите.");
                            break;
                        case 20:
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, "Прекратите флуд.");
                            break;
                        case 30:
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                $"Вам выдан мут до {Operations.time:HH:mm:ss}");
                            return;
                        default:
                        {
                            if (user.countMessages > 30)
                            {
                                return;
                            }
                            break;
                        }
                    }
                }

                var command = BotSettings.commands.FirstOrDefault(_ => _.Compare(message, user));
                if (command != null) await command.Execute(Tgbot, user, message);
                //}
                // catch (Exception ex)
                // {
                //     Console.WriteLine("шибка");
                //     BotSettings.reviews.Enqueue(
                //         $"{e.Message.Chat.Id}:Ошибка у пользователя {e.Message.Chat.Id}: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
                // }
        }
    }
}
