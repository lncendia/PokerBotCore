using System;

namespace PokerBotCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Bot.Start();
            while (true)
            {
                Console.WriteLine("1 - Проверка комнат пользователей.");
                var x = Console.ReadLine();
                switch (x)
                {
                    case "1":
                        Console.WriteLine(Bot.roomsfortest);
                        break;
                    case "2":
                    {
                        Console.WriteLine("Введите колличество игроков");
                        int count = int.Parse(Console.ReadLine() ?? string.Empty);
                        Operation.CreateFakeRoom(count);
                        break;
                    }
                }
            }

        }
    }
}
