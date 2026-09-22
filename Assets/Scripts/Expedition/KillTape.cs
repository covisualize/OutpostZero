namespace OutpostZero.Expedition
{
    /// <summary>
    /// The last three kills, oldest first. A fourth name pushes the oldest off the tape.
    /// </summary>
    public struct KillTape
    {
        public const int Limit = 3;
        public string First;
        public string Second;
        public string Third;
        public int Count;

        public static string Name(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "walker") return "Walker";
            if (id == "runner") return "Runner";
            if (id == "brute") return "Brute";
            return id;
        }

        public void Note(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "Walker";
            if (Count <= 0)
            {
                First = name;
                Count = 1;
                return;
            }
            if (Count == 1)
            {
                Second = name;
                Count = 2;
                return;
            }
            if (Count == 2)
            {
                Third = name;
                Count = 3;
                return;
            }
            First = Second;
            Second = Third;
            Third = name;
        }

        public string Text()
        {
            if (Count <= 0) return "";
            if (Count == 1) return First;
            if (Count == 2) return First + "\n" + Second;
            return First + "\n" + Second + "\n" + Third;
        }
    }
}
