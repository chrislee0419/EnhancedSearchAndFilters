using System.Collections.Generic;

namespace EnhancedSearchAndFilters.Search
{
    internal class Trie
    {
        private class Node
        {
            public string Word = null;
            public bool HasWord { get { return Word != null; } }
            public Dictionary<char, Node> Children = new Dictionary<char, Node>();
        }

        private Node _root = new Node();

        public Trie()
        { }

        public Trie(string[] words)
        {
            foreach (var word in words)
            {
                Node curr = _root;

                for (int i = 0; i < word.Length; ++i)
                {
                    char c = word[i];

                    if (!curr.Children.TryGetValue(c, out var next))
                    {
                        next = new Node();

                        if (i == word.Length - 1)
                            next.Word = word;

                        curr.Children.Add(c, next);
                    }

                    curr = next;
                }
            }
        }

        public void AddWord(string word)
        {
            Node curr = _root;
            int i;

            for (i = 0; i < word.Length; ++i)
            {
                char c = word[i];
                if (!curr.Children.TryGetValue(c, out var next))
                {
                    next = new Node();
                    curr.Children.Add(c, next);
                }

                curr = next;
            }

            curr.Word = word;
        }

        public bool ContainsWord(string word)
        {
            Node curr = _root;

            for (int i = 0; i < word.Length; ++i)
            {
                char c = word[i];
                if (!curr.Children.TryGetValue(c, out var next))
                    return false;

                curr = next;
            }

            return curr.HasWord;
        }

        public List<string> StartsWith(string prefix)
        {
            List<string> words = new List<string>();
            Node curr = _root;

            for (int i = 0; i < prefix.Length; ++i)
            {
                char c = prefix[i];

                // prefix doesn't appear in the trie
                if (!curr.Children.TryGetValue(c, out var next))
                    return words;

                curr = next;
            }

            // get words
            Stack<Node> stack = new Stack<Node>();
            stack.Push(curr);
            while (stack.Count > 0)
            {
                curr = stack.Pop();

                if (curr.HasWord)
                    words.Add(curr.Word);

                foreach (var node in curr.Children.Values)
                    stack.Push(node);
            }

            // remove the prefix from the list if it exists
            words.Remove(prefix);

            return words;
        }
    }
}
