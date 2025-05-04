namespace YatzyServer
{
   

    public class QueuedGame
    {
        Player Player1 { get; set; }
        Player Player2 { get; set; }

        public QueuedGame(Player player1, Player player2)
        {
            if (player1 == null || player2 == null)
            {
                throw new ArgumentNullException("Players cannot be null");
            }
            if (player1 == player2)
            {
                throw new ArgumentException("Players cannot be the same");
            }
            if (player1.Status != Status.Waiting || player2.Status != Status.Waiting)
            {
                throw new InvalidOperationException("Both players must be in the waiting status");
            }
        }

    }

}
