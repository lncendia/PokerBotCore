using System;
using System.Collections.Generic;
using PokerBotCore.Entities;
using PokerBotCore.Rooms;

namespace PokerBotCore.Bot
{
    public static class BuilderFaceRooms
    {
        private static Random Rnd = new Random();
        public static FakeRoom CreateFakeRoom(int count)
        {
            User user = new User {Id = 0, Money = int.MaxValue, state = User.State.wait};
            Room room = new FakeRoom(user, CreateNickname(), count);
            user.room = room;
            return (FakeRoom)room;
        }
        static string a = "bcdfghjklmnpqrstvwxz";
        static string b = "aeiouy";
        static string emoji = "";
        static List<string> c = new List<string>() { a, b };
        static List<string> nameRus = new List<string> { "Александр", "Алексей", "Анатолий", "Андрей", "Антон", "Аркадий", "Арсений", "Артём", "Артур", "Борис", "Вадим", "Валентин", "Валерий", "Василий", "Виктор", "Виталий", "Владимир", "Владислав", "Вячеслав", "Глеб", "Даниил", "Денис", "Дмитрий", "Евгений", "Егор", "Иван", "Игорь", "Илья", "Кирилл", "Константин", "Максим", "Марк", "Матвей", "Михаил", "Никита", "Олег", "Павел", "Пётр", "Роман", "Руслан", "Сергей", "Степан", "Тимур", "Юрец", "Ярик" };
        static List<string> nameEng = new List<string> { "Aleksandr", "Aleksey", "Anatoliy", "Andrey", "Anton", "Arkadiy", "Arseniy", "Artyom", "Artur", "Boris", "Vadim", "Valentin", "Valeriy", "Vasiliy", "Viktor", "Vitaliy", "Vladimir", "Vladislav", "Vyacheslav", "Gleb", "Daniil", "Denis", "Dmitriy", "Evgeniy", "Egor", "Ivan", "Igor", "Il'ya", "Kirill", "Konstantin", "Maksim", "Mark", "Matvey", "Mihail", "Nikita", "Oleg", "Pavel", "Pyotr", "Roma", "Ruslan", "Sergey", "Stepan", "Timur", "Yura", "Yarik" };

        private static string CreateNickname()
        {
            string pass = "";
            switch (Rnd.Next(0, 3))
            {
                case 0:
                    int r = Rnd.Next(0, 2);
                    pass += char.ToUpper(c[r][Rnd.Next(0, c[r].Length)]);
                    int count = Rnd.Next(3, 6);
                    for (int i = r + 1; i < count + r + 1; i++)
                    {
                        pass += c[i % 2][Rnd.Next(0, c[i % 2].Length)];
                    }
                    break;
                case 1:
                    pass = nameRus[Rnd.Next(0, nameRus.Count)];
                    break;
                case 2:
                    pass = nameEng[Rnd.Next(0, nameEng.Count)];
                    break;
            }
            pass += emoji;
            if (Rnd.Next(0, 3) == 2) pass += emoji[Rnd.Next(0, emoji.Length)];
            return pass;
        }
    }
}