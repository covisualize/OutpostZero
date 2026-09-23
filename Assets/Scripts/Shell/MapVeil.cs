using OutpostZero.Graphics;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Campaign-map fog. Ash Market and the roads that touch it are charted from
    /// the first day. A further district stays off the board until it is cleared
    /// or it shares a road with one that is.
    /// </summary>
    public static class MapVeil
    {
        public const string Home = "ash_market";

        public static bool Seen(string id, string[] cleared)
        {
            if (!OnBoard(id)) return false;
            if (id == Home || Beside(id, Home)) return true;
            if (cleared == null) return false;
            for (int i = 0; i < cleared.Length; i++)
            {
                if (string.IsNullOrEmpty(cleared[i])) continue;
                if (cleared[i] == id || Beside(id, cleared[i])) return true;
            }
            return false;
        }

        public static int Hidden(string[] cleared)
        {
            var board = CampaignBoard.All();
            int hidden = 0;
            for (int i = 0; i < board.Length; i++)
            {
                if (!Seen(board[i].Id, cleared)) hidden++;
            }
            return hidden;
        }

        public static string Forecast(string id, string[] cleared, int day)
        {
            if (!Seen(id, cleared)) return "";
            WeatherKind kind = CloudDeck.Lay(DistrictRules.For(id).Weather, day);
            return kind.ToString().ToLowerInvariant();
        }

        public static string Site(string id, string[] cleared)
        {
            if (!Seen(id, cleared)) return "";
            return DistrictBlocks.PoiRole(id);
        }

        private static bool OnBoard(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var board = CampaignBoard.All();
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i].Id == id) return true;
            }
            return false;
        }

        private static bool Beside(string id, string other)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(other) || id == other) return false;
            return Lists(id, other) || Lists(other, id);
        }

        private static bool Lists(string id, string other)
        {
            var board = CampaignBoard.All();
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i].Id != id) continue;
                if (string.IsNullOrEmpty(board[i].Neighbors)) return false;
                var bits = board[i].Neighbors.Split(',');
                for (int n = 0; n < bits.Length; n++)
                {
                    if (bits[n] == other) return true;
                }
                return false;
            }
            return false;
        }
    }
}
