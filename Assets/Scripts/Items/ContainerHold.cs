namespace OutpostZero.Items
{
    /// <summary>
    /// What is still inside a crate. Taking a stack removes it from the crate and leaves the rest.
    /// </summary>
    public static class ContainerHold
    {
        public struct Stack
        {
            public string Id;
            public int Count;
        }

        public static Stack[] Take(Stack[] stacks, string id, int count, out int moved)
        {
            moved = 0;
            if (stacks == null || string.IsNullOrEmpty(id) || count <= 0) return stacks ?? new Stack[0];
            var next = new Stack[stacks.Length];
            int write = 0;
            for (int i = 0; i < stacks.Length; i++)
            {
                var stack = stacks[i];
                if (stack.Id != id || moved > 0)
                {
                    if (stack.Count > 0) next[write++] = stack;
                    continue;
                }
                int pull = count < stack.Count ? count : stack.Count;
                moved = pull;
                stack.Count -= pull;
                if (stack.Count > 0) next[write++] = stack;
            }
            if (write == stacks.Length && moved == 0) return stacks;
            var trimmed = new Stack[write];
            for (int i = 0; i < write; i++) trimmed[i] = next[i];
            return trimmed;
        }

        public static Stack[] TakeAll(Stack[] stacks, out Stack[] moved)
        {
            if (stacks == null || stacks.Length == 0)
            {
                moved = new Stack[0];
                return new Stack[0];
            }
            int kept = 0;
            for (int i = 0; i < stacks.Length; i++)
            {
                if (stacks[i].Count > 0) kept++;
            }
            moved = new Stack[kept];
            int write = 0;
            for (int i = 0; i < stacks.Length; i++)
            {
                if (stacks[i].Count <= 0) continue;
                moved[write++] = stacks[i];
            }
            return new Stack[0];
        }

        public static string Signature(Stack[] stacks)
        {
            if (stacks == null || stacks.Length == 0) return "";
            string text = "";
            for (int i = 0; i < stacks.Length; i++)
            {
                if (i > 0) text += "|";
                text += stacks[i].Id + "*" + stacks[i].Count;
            }
            return text;
        }
    }
}
