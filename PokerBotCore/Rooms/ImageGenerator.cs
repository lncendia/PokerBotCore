using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PokerBotCore.Model;

namespace PokerBotCore.Rooms
{
    public static class ImageGenerator
    {
        private static string GetNameFile(string card)
        {
            sbyte nominal1 = CardsOperation.GetNominal(card);
            string nominal;
            switch (nominal1)
            {
                case 11:
                    nominal = "jack";
                    break;
                case 12:
                    nominal = "queen";
                    break;
                case 13:
                    nominal = "king";
                    break;
                case 14:
                    nominal = "ace";
                    break;
                default:
                    nominal = nominal1.ToString();
                    break;
            }

            string suitStr = card.Substring(card.Length - 1), suit = "";
            switch (suitStr)
            {
                case "♣":
                    suit = "clubs";
                    break;
                case "♠":
                    suit = "spades";
                    break;
                case "♥":
                    suit = "hearts";
                    break;
                case "♦":
                    suit = "diamonds";
                    break;
            }

            return $"{nominal}_of_{suit}.png";
        }

        public static Image GetImage(List<string> cards, User user)
        {
            var table = Image.FromFile(File.Exists($"tables\\{user.Id}.jpg")
                ? $"tables\\{user.Id}.jpg"
                : "tables\\table.jpg");
            Graphics g = Graphics.FromImage(table);
            List<Image> images = cards.Select(str => Image.FromFile($"cards\\{GetNameFile(str)}")).ToList();

            int x = 50;
            int y = (table.Width - 25) / 5;
            for (int i = 0; i < images.Count - 2; i++)
            {
                g.DrawImage(images[i], x + y * i, 75);
            }

            g.DrawImage(images[^2], 520, 545);
            g.DrawImage(images[^1], 820, 545);
            return table;
        }
    }
}