using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using PokerBotCore.Entities;
using PokerBotCore.Keyboards;
using PokerBotCore.Payments;
using PokerBotCore.Rooms;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using User = PokerBotCore.Entities.User;

namespace PokerBotCore.Bot
{
    internal static class MainBot
    {
        public static readonly TelegramBotClient Tgbot = new("1341769299:AAE4q84mx-NRrSJndKsCVNVLr-SzjYeN7wk");
        public static readonly List<Room> Rooms = new();
        private static readonly List<User> Chat = new();
        public static List<User> users;
        private static List<Friendship> _friendships;
        public static readonly List<FakeRoom> FakeRooms = new();
        public static readonly Queue<string> Reviews = new();

        public static void Start()
        {
            using Db db = new Db();
            users = db.Users.ToList();
            _friendships = db.Friendships.ToList();
            db.Dispose();
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnMessage += Admin.Tgbot_Admin;
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
                User user = users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;
                if (user.state == User.State.admin || user.state == User.State.countFakeRoom) return;
                if (cb.Contains("public") && user.state == User.State.waitCount)
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

                if (cb.Contains("private") && user.state == User.State.waitCount)
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

                    User info = users.FirstOrDefault(x => x.Id == id);
                    if (info == null)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Пользователь не найден.");
                        return;
                    }

                    var f = _friendships.ToList().FirstOrDefault(friendship =>
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
                    case "sentroom":
                        if (user.state != User.State.wait)
                        {
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                            return;
                        }

                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            e.CallbackQuery.Message.Text, replyMarkup: MainKeyboards.CreatePrivateRoomKeyboard);
                        SendMessageToChat($"Приглашаю вас в комнату {user.room.id}.", e.CallbackQuery.From.Username,
                            user, MainKeyboards.CreateConnectButton(user.room));
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Приглашение отправлено.");
                        break;
                    case "Raise":
                        if (user.state != User.State.waitBet) return;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            $"Введите колличество. На вашем счету {user.Money} коинов. Максимальная ставка: 1000 коинов.");
                        break;
                    case "Call":
                    {
                        if (user.state != User.State.waitBet) return;
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
                        if (user.state != User.State.waitBet) return;
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
                        if (user.state != User.State.waitBet) return;

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
                        if (user.state != User.State.waitBet) return;
                        user.combination = null;
                        user.room.foldUsers.Add(user);
                        user.lastRaise = 0;
                        if (user.state == User.State.waitBet) user.room.next = true;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                            "Ход переходит к следующему игроку.");
                        user.room.SendMessage($"Игрок {user.firstName} сбросил карты.", user.room.players, null);
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "change_table":
                        if (user.state != User.State.main) return;
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

                        user.state = User.State.changeTable;

                        break;
                    case "standard_table":
                        if (user.state == User.State.changeTable || user.state == User.State.main)
                        {
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                            File.Delete($"tables\\{user.Id}.jpg");
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Фон изменен на стандартный.");
                            user.state = User.State.main;
                        }
                        else
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Невозможно изменить фон сейчас.");
                        }

                        break;
                    case "friends":
                        if (user.state != User.State.main) return;
                        var f = _friendships.ToList().Where(friendship =>
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
                            var friendUser = users.FirstOrDefault(x => x.Id == friend.User2);
                            string online = friendUser != null && friendUser.countMessages > 0
                                ? "В сети"
                                : "Не в сети";
                            if (friendUser?.room != null && friendUser.state == User.State.wait)
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
                        if (user.state != User.State.main) return;
                        f = _friendships.ToList().Where(friendship =>
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
                                var friendUser = users.FirstOrDefault(x => x.Id == friend.User2);
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
                        if (user.state != User.State.main && user.state != User.State.chat) return;
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
                            if (user.state == User.State.chat)
                                await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы покинули чат.");
                            if (room.key != 0)
                            {
                                user.idPrivateRoom = idRoom;
                                user.state = User.State.codPrivate;
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
                Reviews.Enqueue(
                    $"Ошибка у пользователя {e.CallbackQuery.From.Id}: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
            }
        }

        private static async void Tgbot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                var message = e.Message;
                if (message.Type != MessageType.Text && message.Type != MessageType.Photo) return;
                var user = Operations.GetUser(message.From.Id);
                if (user != null)
                {
                    if (user.state == User.State.answer ||
                        user.state == User.State.mailing || user.state == User.State.addCoin) return;
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

                if (message.Type == MessageType.Text)
                {
                    if (message.Text.Contains("/start"))
                    {
                        if (user != null)
                        {
                            WorkWithLinks(message, user);
                            return;
                        }
                        await using Db db = new Db();
                        User refer;
                        try
                        {
                            long id = long.Parse(message.Text.Split(' ')[1]);
                            refer = users.FirstOrDefault(x => x.Id == id);
                            if (refer != null && refer.Id == message.Chat.Id) refer = null;
                        }
                        catch
                        {
                            refer = null;
                        }

                        if (refer != null)
                        {
                            try
                            {
                                await Tgbot.SendTextMessageAsync(refer.Id,
                                    $"По вашей реферальной ссылке подключился игрок @{message.From.Username}.");
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        var userToAdd = new User() {Id = message.From.Id, Money = 0, Referal = refer};
                        users.Add(userToAdd);
                        if (refer != null) db.Update(refer);
                        await db.Users.AddAsync(userToAdd);
                        await db.SaveChangesAsync();
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            $"Добро пожаловать! Пополни свой счет и вперед играть!\nТвоя реферальная ссылка: https://t.me/PokerGame777_bot?start={message.From.Id} \nЗа каждого приглашенного игрока вы будете получать 7% от его пополнений.",
                            replyMarkup: MainKeyboards.MainKeyboard);
                        return;
                    }
                    if (user == null) return;
                    switch (message.Text)
                    {
                        case "🥊Создать комнату":
                            if (user.room!=null) return;
                            if (user.Money < 40) //////////////////////////////
                            {
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    "Недостаточно средств. Счет должен быть больше 40 коинов.");
                                return;
                            }

                            user.state = User.State.waitCount;
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите количество мест. От 2 до 5.");
                            break;
                        case "🃏Список комнат":
                            if (user.room!=null) return;
                            user.state = User.State.main;
                            var key = MainKeyboards.CreateConnectButton(Rooms);
                            if (key.InlineKeyboard.Count() != 0)
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    "Показаны первые 50 комнат. Вы можете ввести ID нужной вам комнаты.",
                                    replyMarkup: key);
                            else
                                await Tgbot.SendTextMessageAsync(message.Chat.Id, "Комнаты не найдены.");
                            break;
                        case "Выход":
                            if (user.room!=null)
                            {
                                await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вы уверены?",
                                    replyMarkup: MainKeyboards.AreYouSure);
                            }

                            break;
                        case "🎲Пополнить счет":
                            if (user.room!=null) return;
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Введите сумму, на которую хотите пополнить баланс.");
                            user.state = User.State.waitMoney;
                            break;
                        case "💸Вывод":
                            if (user.room!=null) return;
                            user.state = User.State.output;
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите сумму, которую хотите вывести.");
                            break;
                        case "⁉Оставить отзыв":
                            if (user.room!=null) return;
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Напишите отзыв. Он будет отпрален автору бота.");
                            user.state = User.State.feedback;
                            break;
                        case "👤Профиль":
                            if (user.room!=null) return;
                            user.state = User.State.main;
                            string str =
                                $"Ваш ID: {user.Id}\nВаши средства: {user.Money}\nВаша реферальная ссылка: https://t.me/PokerGame777_bot?start={message.From.Id}";
                            if (user.Referal != null)
                            {
                                var user2 = await Tgbot.GetChatMemberAsync((int) user.Referal.Id,
                                    (int) user.Referal.Id);
                                str += $"\nВас пригласил: @{user2.User.Username}";
                            }

                            await Tgbot.SendTextMessageAsync(message.Chat.Id, str,
                                replyMarkup: MainKeyboards.ProfileKeyboard);
                            break;
                        case "📬Игровой чат":
                            if (user.room!=null || user.state == User.State.chat) return;
                            user.state = User.State.chat;
                            Chat.Add(user);
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вы вошли в игровой чат.",
                                replyMarkup: new ReplyKeyboardMarkup(new List<KeyboardButton>()
                                    {new KeyboardButton("Покинуть чат")}));
                            break;
                        case "/admin":
                            if (e.Message.From.Id != 346978522) return;
                            if (user.state == User.State.admin)
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id, "Вы вышли из меню админа.",
                                    replyMarkup: MainKeyboards.MainKeyboard);
                                user.state = User.State.main;
                            }
                            else
                            {
                                user.state = User.State.admin;
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id,
                                    "Добро пожаловать в админ-панель.",
                                    replyMarkup: MainKeyboards.AdminKeyboard);
                            }

                            break;
                        default:
                            int raise;
                            switch (user.state)
                            {
                                case User.State.waitCount:
                                    int count;
                                    try
                                    {
                                        count = Int32.Parse(message.Text);
                                    }
                                    catch
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }

                                    if (count < 2 || count > 5)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Написано же! От 2 до 5.");
                                        return;
                                    }

                                    await Tgbot.SendTextMessageAsync(message.Chat.Id, "Выберете тип комнаты:",
                                        replyMarkup: MainKeyboards.CreateRoomSelect(count));
                                    break;
                                case User.State.main:
                                    int id;
                                    try
                                    {
                                        id = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }

                                    Room room = Operations.GetRoom(id);
                                    if (room == null || room.started)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Комната не существует или игра в ней уже началась.");
                                        return;
                                    }

                                    await Tgbot.SendTextMessageAsync(message.Chat.Id, "Список комнат:",
                                        replyMarkup: MainKeyboards.CreateConnectButton(room));
                                    break;
                                case User.State.waitBet:
                                {
                                    try
                                    {
                                        raise = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }

                                    if (raise > 1000)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Ставка не может быть больше 1000 коинов.");
                                        return;
                                    }

                                    if (raise > user.Money)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Недостаточно средств!");
                                        return;
                                    }

                                    if (raise < 25) /////////////////////////////////////////////////
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Ставка должна быть больше 25 коинов.");
                                        return;
                                    }

                                    await using Db db = new Db();
                                    int raise1 = user.room.lastRaise - user.lastRaise + raise;
                                    Console.WriteLine(raise1);
                                    user.bet += raise1;
                                    user.room.lastRaise += raise;
                                    user.room.bet += raise1;
                                    user.Money -= raise1;
                                    user.lastRaise += raise1;
                                    if (user.Money == 0) user.room.allInUsers.Add(user);
                                    user.room.next = true;
                                    db.UpdateRange(user);
                                    await db.SaveChangesAsync();
                                    user.room.SendMessage($"Игрок {user.firstName} повысил ставку на {raise} коинов.",
                                        user.room.players, null);
                                    break;
                                }
                                case User.State.waitMoney:
                                    try
                                    {
                                        raise = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }

                                    if (raise > 99999)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Слишком большая сумма!");
                                        return;
                                    }

                                    if (raise < 30)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Сумма должна быть больше 30 рублей!");
                                        return;
                                    }

                                    var billId = "";
                                    var payUrl = Transactions.NewTransaction(raise, user, ref billId);
                                    if (payUrl == null)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Произошла ошибка. Попробуйте еще раз.");
                                        return;
                                    }

                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        $"Пополнение счета на сумму {raise} р.\nДата: {DateTime.Now:dd.MMM.yyyy}\nСтатус: Не оплачено.\n\nОплатите счет по ссылке.\n{payUrl}",
                                        replyMarkup: MainKeyboards.CheckBill(billId));
                                    user.state = User.State.main;
                                    break;
                                case User.State.output:
                                    int money;
                                    try
                                    {
                                        money = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }

                                    if (money > user.Money)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Недостаточно средств.");
                                        return;
                                    }

                                    if (money < 50) //TO DO: Ограничение на вывод.
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Вывод средств осуществляется от 50 рублей.");
                                        return;
                                    }

                                    user.output = money;
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Введите номер QIWI кошелька, на который будет осуществляться вывод.\nФормат: +<код страны><номер>");
                                    user.state = User.State.outputWaitNumber;
                                    break;
                                case User.State.outputWaitNumber:
                                    if (!message.Text.Contains("+"))
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Номер должен начинаться с \"+\"");
                                        return;
                                    }

                                    bool success = Transactions.OutputTransaction(message.Text, user);
                                    user.output = 0;
                                    user.state = User.State.main;
                                    if (success)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Запрос отправлен.");
                                    }
                                    else
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Произошла ошибка. Попробуйте позже.");
                                    }

                                    break;
                                case User.State.codPrivate:
                                    int keyRoom;
                                    try
                                    {
                                        keyRoom = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }

                                    room = Operations.GetRoom(user.idPrivateRoom);
                                    if (room == null)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                            "Комната не доступна для подключения. Возможно игра в ней уже началась.");
                                        user.idPrivateRoom = 0;
                                        return;
                                    }

                                    if (room.key != 0 && room.key == keyRoom)
                                    {
                                        room.AddPlayer(user, message.Chat.FirstName);
                                        user.idPrivateRoom = 0;
                                    }
                                    else
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Пароль неверный.");
                                    }

                                    break;
                                case User.State.feedback:
                                    user.state = User.State.main;
                                    Reviews.Enqueue($"{user.Id}:{message.Chat.FirstName} ({user.Id}): {message.Text}");
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Спасибо за отзыв. Мы рассмотрим его в ближайшее время.");
                                    break;
                                case User.State.chat:
                                    if (message.Text.Equals("Покинуть чат"))
                                    {
                                        user.state = User.State.main;
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вы покинули чат.",
                                            replyMarkup: MainKeyboards.MainKeyboard);
                                        break;
                                    }

                                    SendMessageToChat(message.Text, message.From.Username, user, null);
                                    break;
                            }

                            break;
                    }
                }

                if (user == null || user.state != User.State.changeTable) return;
                if (message.Type != MessageType.Photo)
                {
                    await Tgbot.SendTextMessageAsync(message.Chat.Id, "Отправьте фотографию!");
                    return;
                }

                user.state = User.State.main;
                await using (var ms = new MemoryStream())
                {
                    await Tgbot.GetInfoAndDownloadFileAsync(message.Photo[^1].FileId, ms);
                    Image image = Image.FromStream(ms);
                    var bmp = new Bitmap(image, 1590, 960);
                    bmp.Save($"tables\\{user.Id}.jpg");
                    bmp.Dispose();
                    image.Dispose();
                }
                await Tgbot.SendTextMessageAsync(message.Chat.Id, "Успешно.");
            }
            catch (Exception ex)
            {
                Reviews.Enqueue(
                    $"{e.Message.Chat.Id}:Ошибка у пользователя {e.Message.Chat.Id}: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
            }
        }

        private static async void AddFriend(Message message, User user)
        {
            User info;
            await using Db db = new Db();
            try
            {
                string information = message.Text.Split(' ')[1];
                info = users.FirstOrDefault(
                    x => x.Id == long.Parse(information.Substring(information.IndexOf('_') + 1)));
                if (info == null) return;
                if (info.Id == message.Chat.Id) return;
            }
            catch
            {
                return;
            }

            var f = _friendships.ToList().FirstOrDefault(friendship =>
                (friendship.User1 == user.Id && friendship.User2 == info.Id) ||
                (friendship.User1 == info.Id && friendship.User2 == user.Id));
            if (f != null) return;
            db.UpdateRange(user, info);
            Friendship friend = new Friendship {User1 = user.Id, User2 = info.Id, Accepted = false};
            _friendships.Add(friend);
            await db.Friendships.AddAsync(friend);
            await db.SaveChangesAsync();
            await Tgbot.SendTextMessageAsync(user.Id, $"Пользователю отправлена заявка в друзья.");
            await Tgbot.SendTextMessageAsync(info.Id,
                $"Пользователь @{message.From.Username} отправил вам заявку в друзья.",
                replyMarkup: MainKeyboards.FriendsRequest(user.Id));
        }

        private static async void SendMessageToChat(string message, string username, User user, IReplyMarkup markup)
        {
            foreach (User user1 in Chat.ToList())
            {
                try
                {
                    if (user1 == user) continue;
                    await Tgbot.SendTextMessageAsync(user1.Id, $"@{username}(<a href =\"https://telegram.me/PokerGame777_bot?start=info_{user.Id}\">+</a>): {message}", replyMarkup: markup, parseMode:ParseMode.Html);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static async void WorkWithLinks(Message message, User user)
        {
            try
            {
                if (message.Text.Contains("info_"))
                {
                    AddFriend(message, user);
                    return;
                }

                if (message.Text.Contains("connect_"))
                {
                    int idRoom = Int32.Parse(message.Text.Split('_')[1]);
                    Room room = Rooms.Find((room1 => room1.id == idRoom));
                    if (room == null) return;
                    await Tgbot.SendTextMessageAsync(user.Id, $"Комната вашего друга:",
                        replyMarkup: MainKeyboards.CreateConnectButton(room));
                    return;
                }

                await using Db db = new Db();
                if (!message.Text.Contains("remove_")) return;
                long id = long.Parse(message.Text.Split('_')[1]);
                Friendship friendship = _friendships.FirstOrDefault(x => x.Id == id);
                if (friendship == null) return;
                if (friendship.User1 == user.Id || friendship.User2 == user.Id)
                {
                    _friendships.Remove(friendship);
                    db.Friendships.Remove(friendship);
                    await db.SaveChangesAsync();
                }

                await Tgbot.SendTextMessageAsync(user.Id, $"Друг удален.");
            }
            catch
            {
                // ignored
            }
        }
    }
}
