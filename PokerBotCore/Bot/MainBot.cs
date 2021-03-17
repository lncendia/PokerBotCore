using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using PokerBotCore.Enums;
using PokerBotCore.Keyboards;
using PokerBotCore.Model;
using PokerBotCore.Payments;
using PokerBotCore.Rooms;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using File = System.IO.File;
using User = PokerBotCore.Model.User;

namespace PokerBotCore.Bot
{
    internal static class MainBot
    {
        public static readonly TelegramBotClient Tgbot = BotSettings.Get();

        public static void Start()
        {
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Tgbot.OnCallbackQuery += Admin.Tgbot_AdminCallbackQuery;
            Operations.Mute();
            Tgbot.StartReceiving();
        }

        private static async void Tgbot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            try
            {
                var cb = e.CallbackQuery.Data;
                User user = BotSettings.users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;
                if (user.state == State.admin || user.state == State.enterCountPlayersOfFakeRoom) return;
                if (cb.Contains("public") && user.state == State.enterCountPlayers)
                {
                    int count = Int32.Parse(cb.Substring(7));
                    Room room = Operations.CreateRoom(count, user,false);
                    user.firstName = e.CallbackQuery.From.FirstName;
                    await Tgbot.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                    await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                        $"Создана комната с ID {room.id}. Ожидаем подключения других игроков.",
                        replyMarkup: MainKeyboards.CreateRoomKeyboard);
                    return;
                }

                if (cb.Contains("private") && user.state == State.enterCountPlayers)
                {
                    int count = Int32.Parse(cb.Substring(8));
                    Room room = Operations.CreateRoom(count, user, true);
                    user.firstName = e.CallbackQuery.From.FirstName;
                    await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                        $"Создана комната с ID {room.id}. Ожидаем подключения других игроков.",
                        replyMarkup: MainKeyboards.CreatePrivateRoomKeyboard);
                    return;
                }

                if (cb.StartsWith("bill"))
                {
                    if (!Transactions.CheckPay(user, cb.Substring(5))) return;
                    string message = e.CallbackQuery.Message.Text;
                    message = message.Replace("Не оплачено", "Оплачено");
                    await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                        message);
                    return;
                }

                if (cb.StartsWith("Add"))
                {
                    long id;
                    try
                    {
                        id = long.Parse(cb.Substring(4));
                    }
                    catch
                    {
                        return;
                    }

                    User info = BotSettings.users.FirstOrDefault(x => x.Id == id);
                    if (info == null)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Пользователь не найден.");
                        return;
                    }

                    var f = BotSettings.friendships.ToList().FirstOrDefault(friendship =>
                        (friendship.User1 == user.Id && friendship.User2 == info.Id) ||
                        (friendship.User1 == info.Id && friendship.User2 == user.Id));
                    if (f == null)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                            "Заявка была отклонена пользователем.");
                        return;
                    }

                    if (f.Accepted)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы уже друзья.");
                        return;
                    }

                    await using Db db = new Db();
                    db.UpdateRange(user, f);
                    f.Accepted = true;
                    await db.SaveChangesAsync();
                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Запрос принят.");
                    return;
                }

                switch (cb)
                {
                    case "exit":
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        if (user.room != null)
                        {
                            while (user.room.block)
                            {
                            }

                            user.room.UserLeave(user);
                        }

                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Вы покинули комнату.");
                        break;
                    // case "sentroom":
                    //     if (user.state != State.wait)
                    //     {
                    //         await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    //         return;
                    //     }
                    //
                    //     await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                    //         e.CallbackQuery.Message.Text, replyMarkup: MainKeyboards.CreatePrivateRoomKeyboard);
                    //     SendMessageToChat($"Приглашаю вас в комнату {user.room.id}.", e.CallbackQuery.From.Username,
                    //         user, MainKeyboards.CreateConnectButton(user.room));
                    //     await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Приглашение отправлено.");
                    //     break;
                    // case "Raise":
                    //     if (user.state != State.waitBet) return;
                    //     await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                    //         $"Введите колличество. На вашем счету {user.Money} коинов. Максимальная ставка: 1000 коинов.");
                    //     break;
                    case "Call":
                    {
                        if (user.state != State.waitBet) return;
                        await using Db db = new Db();
                        var x = user.room.lastRaise - user.lastRaise;
                        if (user.Money >= x)
                        {
                            user.Money -= x;
                            user.room.bet += x;
                            user.lastRaise += x;
                            user.bet += x;
                            if (user.Money == 0) user.room.allInUsers.Add(user);
                            user.room.next = true;
                            db.UpdateRange(user);
                            await db.SaveChangesAsync();
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Ход переходит к следующему игроку.");
                        }
                        else
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, $"Недостаточно средств!",
                                replyMarkup: GameKeyboards.VaBank);
                        }

                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    }
                    case "VA-Bank":
                        if (user.state != State.waitBet) return;
                        if (user.Money < user.room.lastRaise - user.lastRaise)
                        {
                            await using Db db = new Db();
                            user.room.allInUsers.Add(user);
                            user.room.bet += user.Money;
                            user.bet += user.Money;
                            user.lastRaise += user.Money;
                            user.Money = 0;
                            user.room.next = true;
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Ход переходит к следующему игроку.");
                            db.UpdateRange(user);
                            await db.SaveChangesAsync();
                            user.room.SendMessage($"Игрок {user.firstName} пошел ва-банк.", user.room.players,
                                null);
                        }

                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "Check":
                        if (user.state != State.waitBet) return;

                        if (user.room.lastRaise == 0 || user.room.lastRaise - user.lastRaise == 0)
                        {
                            user.room.next = true;
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Ход переходит к следующему игроку.");
                        }
                        else
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Отличная попытка схитрить... Но нет.", true);
                            break;
                        }

                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "Fold":
                        if (user.state != State.waitBet) return;
                        user.combination = null;
                        user.room.foldUsers.Add(user);
                        user.lastRaise = 0;
                        if (user.state == State.waitBet) user.room.next = true;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                            "Ход переходит к следующему игроку.");
                        user.room.SendMessage($"Игрок {user.firstName} сбросил карты.", user.room.players, null);
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "change_table":
                        if (user.state != State.main) return;
                        var image = Image.FromFile(File.Exists($"tables\\{user.Id}.jpg")
                            ? $"tables\\{user.Id}.jpg"
                            : $"tables\\table.jpg");
                        await using (var ms = new MemoryStream())
                        {
                            image.Save(ms, ImageFormat.Jpeg);
                            image.Dispose();
                            ms.Position = 0;
                            await Tgbot.SendPhotoAsync(e.CallbackQuery.From.Id, new InputOnlineFile(ms),
                                caption: "Ваш нынешний фон.");
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Отправьте фотографию фона, который хотите установить.",
                                replyMarkup: MainKeyboards.StandartTable);
                        }

                        user.state = State.enterPhotoTable;

                        break;
                    case "standard_table":
                        if (user.state == State.enterPhotoTable || user.state == State.main)
                        {
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                            File.Delete($"tables\\{user.Id}.jpg");
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Фон изменен на стандартный.");
                            user.state = State.main;
                        }
                        else
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Невозможно изменить фон сейчас.");
                        }

                        break;
                    case "friends":
                        if (user.state != State.main) return;
                        var f = BotSettings.friendships.ToList().Where(friendship =>
                            (friendship.User1 == user.Id || friendship.User2 == user.Id)&&friendship.Accepted).ToList();
                        if (!f.Any())
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "У вас нет друзей :(",
                                replyMarkup: MainKeyboards.SentedRequest);
                            return;
                        }

                        string friends = "Ваши друзья:\n";
                        foreach (Friendship friend in f)
                        {
                            int id = friend.User1 == user.Id ? (int) friend.User2 : (int) friend.User1;
                            var user2 = await Tgbot.GetChatMemberAsync(id, id);
                            var friendUser = BotSettings.users.FirstOrDefault(x => x.Id == friend.User2);
                            string online = friendUser != null && friendUser.countMessages > 0
                                ? "В сети"
                                : "Не в сети";
                            if (friendUser?.room != null && friendUser.state == State.wait)
                            {
                                friends +=
                                    $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.Id}\">-</a>)@{user2.User.Username} (<a href =\"https://telegram.me/PokerGame777_bot?start=connect_{friendUser.room.id}\">В игре</a>)\n";
                            }
                            else
                                friends +=
                                    $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.Id}\">-</a>)@{user2.User.Username} ({online})\n";
                        }

                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, friends,
                            replyMarkup: MainKeyboards.SentedRequest,
                            parseMode: ParseMode.Html);

                        break;
                    case "sentedRequest":
                        if (user.state != State.main) return;
                        f = BotSettings.friendships.ToList().Where(friendship =>
                                (friendship.User1 == user.Id || friendship.User2 == user.Id) &&
                                !friendship.Accepted)
                            .ToList();
                        if (!f.Any())
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "У вас нет непринятых заявок.");
                        }
                        else
                        {
                            friends = "Ваши заякви:\n";
                            foreach (Friendship friend in f)
                            {
                                int id = friend.User1 == user.Id ? (int)friend.User2 : (int)friend.User1;
                                var user2 = await Tgbot.GetChatMemberAsync(id, id);
                                var friendUser = BotSettings.users.FirstOrDefault(x => x.Id == friend.User2);
                                string online = friendUser != null && friendUser.countMessages > 0
                                    ? "В сети"
                                    : "Не в сети";
                                friends +=
                                    $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.Id}\">-</a>)@{user2.User.Username} ({online})\n";
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, friends,
                                    replyMarkup: MainKeyboards.SentedRequest,
                                    parseMode: ParseMode.Html);
                            }
                        }

                        break;
                    default:
                        if (user.state != State.main) return;
                        int idRoom;
                        idRoom = int.Parse(cb);
                        if (user.Money < 40) ////////////////////////////////////////////////////////////
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Недостаточно средств. Счет должен быть больше 40 коинов.");
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                            return;
                        }
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        var room = Operations.GetRoom(Convert.ToInt32(cb));
                        if (room != null&&!room.started)
                        {
                            if (room.key != 0)
                            {
                                user.idPrivateRoom = idRoom;
                                user.state = State.enterPassword;
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, $"Введите пароль.");
                            }
                            else
                            {
                                room.AddPlayer(user, e.CallbackQuery.From.FirstName);
                            }
                        }
                        else
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Комната не доступна для подключения. Возможно игра в ней уже началась.");
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                BotSettings.reviews.Enqueue(
                    $"Ошибка у пользователя {e.CallbackQuery.From.Id}: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
            }
        }
        
        private static async void Tgbot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
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
