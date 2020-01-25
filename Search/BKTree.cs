using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace EnhancedSearchAndFilters.Search
{
    // adapted from: https://nullwords.wordpress.com/2013/03/13/the-bk-tree-a-data-structure-for-spell-checking/
    internal class BKTree
    {
        private Node _root;

        public void AddWord(string word)
        {
            if (_root == null)
            {
                _root = new Node(word);
                return;
            }

            Node curr = _root;

            int dist = FuzzyStringMatching.LevenshteinDistance(curr.Word, word);
            while (curr.ContainsKey(dist))
            {
                if (dist == 0)
                    return;

                curr = curr[dist];
                dist = FuzzyStringMatching.LevenshteinDistance(curr.Word, word);
            }

            curr.AddChild(dist, word);
        }

        public List<string> Search(string word, int tolerance = 2)
        {
            List<string> results = new List<string>();

            if (word == null || word.Length == 0 || _root == null)
                return results;

            Queue<Node> nodesToSearch = new Queue<Node>();
            nodesToSearch.Enqueue(_root);

            while (nodesToSearch.Count > 0)
            {
                Node curr = nodesToSearch.Dequeue();
                int dist = FuzzyStringMatching.LevenshteinDistance(curr.Word, word);
                int minDist = dist - tolerance;
                int maxDist = dist + tolerance;

                if (dist <= tolerance)
                    results.Add(curr.Word);

                foreach (int key in curr.Keys.Where(key => key >= minDist && key <= maxDist))
                    nodesToSearch.Enqueue(curr[key]);
            }

            return results;
        }

        private class Node
        {
            public string Word { get; set; }
            public HybridDictionary Children { get; private set; }

            public Node()
            { }

            public Node(string word)
            {
                Word = word;
            }

            public Node this[int key]
            {
                get => (Node)Children[key];
            }

            public IEnumerable<int> Keys
            {
                get
                {
                    if (Children == null)
                        return new List<int>();
                    return Children.Keys.Cast<int>();
                }
            }

            public bool ContainsKey(int key) => Children != null && Children.Contains(key);

            public void AddChild(int key, string word)
            {
                if (Children == null)
                    Children = new HybridDictionary();
                Children[key] = new Node(word);
            }
        }
    }
}
