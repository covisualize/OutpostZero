using System.Collections.Generic;

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

        public static Stack[] Put(Stack[] stacks, string id, int count)
        {
            if (stacks == null) stacks = new Stack[0];
            if (string.IsNullOrEmpty(id) || count <= 0) return stacks;
            var next = new Stack[stacks.Length];
            for (int i = 0; i < stacks.Length; i++)
            {
                next[i] = stacks[i];
                if (next[i].Id != id) continue;
                next[i].Count += count;
                return next;
            }
            var grown = new Stack[stacks.Length + 1];
            for (int i = 0; i < stacks.Length; i++) grown[i] = stacks[i];
            grown[stacks.Length] = new Stack { Id = id, Count = count };
            return grown;
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

        public static string Encode(Stack[] stacks)
        {
            if (stacks == null || stacks.Length == 0) return "";
            string text = "";
            for (int i = 0; i < stacks.Length; i++)
            {
                if (string.IsNullOrEmpty(stacks[i].Id) || stacks[i].Count <= 0) continue;
                if (text.Length > 0) text += ";";
                text += stacks[i].Id + "*" + stacks[i].Count;
            }
            return text;
        }

        public static Stack[] Decode(string body)
        {
            if (string.IsNullOrEmpty(body)) return new Stack[0];
            var bits = body.Split(';');
            var list = new List<Stack>();
            for (int i = 0; i < bits.Length; i++)
            {
                if (string.IsNullOrEmpty(bits[i])) continue;
                var pair = bits[i].Split('*');
                if (pair.Length < 2 || string.IsNullOrEmpty(pair[0])) continue;
                int.TryParse(pair[1], out int count);
                if (count <= 0) continue;
                list.Add(new Stack { Id = pair[0], Count = count });
            }
            return list.ToArray();
        }

        public static string Offer(Stack stack)
        {
            if (string.IsNullOrEmpty(stack.Id) || stack.Count <= 0) return "";
            var record = ItemCatalog.Find(stack.Id);
            string name = record != null && !string.IsNullOrEmpty(record.DisplayName) ? record.DisplayName : stack.Id;
            return name + " x" + stack.Count;
        }
    }
}
