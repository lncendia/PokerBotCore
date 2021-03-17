using System.Collections.Generic;

namespace PokerBotCore.Rooms
{
    public static class CardsOperation
    {
        public static sbyte GetNominal(string card)
        {
            return card.Remove(card.Length - 2) switch
            {
                "Два" => 2,
                "Три" => 3,
                "Четыре" => 4,
                "Пять" => 5,
                "Шесть" => 6,
                "Семь" => 7,
                "Восемь" => 8,
                "Девять" => 9,
                "Десять" => 10,
                "Валет" => 11,
                "Дама" => 12,
                "Король" => 13,
                "Туз" => 14,
                _ => 0
            };
        }

        public static void Sort(List<char> cd, List<sbyte> keys)
        {
            for (int i = 0; i < keys.Count - 1; i++)
            {
                for (int j = i + 1; j < keys.Count; j++)
                {
                    if (keys[i] > keys[j])
                    {
                        sbyte vol = keys[i];
                        keys[i] = keys[j];
                        keys[j] = vol;
                        char s = cd[i];
                        cd[i] = cd[j];
                        cd[j] = s;
                    }
                }
            }
        }

    }
}