namespace PokerBotCore.Model
{
    public class Friendship
    {
        public int Id { get; set; }
        public long User1 { get; set; }
        public long User2 { get; set; }
        public bool Accepted { get; set; }
    }
}
