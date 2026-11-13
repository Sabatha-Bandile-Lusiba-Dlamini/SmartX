namespace SmartX.Api.DataStructures;


// Self-balancing binary search tree (AVL). Keys are ordered, so a range query only walks the part of the
// tree that can contain matches: O(log n + k) instead of scanning every reading (O(n)).
// Readings with an identical key (two events with the same timestamp) share one node.

public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private sealed class Node
    {
        public TKey Key;
        public List<TValue> Values = new();
        public Node? Left;
        public Node? Right;
        public int Height = 1;

        public Node(TKey key, TValue value)
        {
            Key = key;
            Values.Add(value);
        }
    }

    private Node? _root;

    //Total number of stored values (not nodes).
    public int Count { get; private set; }

    public int Height => H(_root);

    public void Insert(TKey key, TValue value)
    {
        _root = Insert(_root, key, value);
        Count++;
    }

    
    // Returns every value whose key is between from and to (inclusive), in ascending key order.
    // nodesVisited reports how many tree nodes were touched, to show the efficiency gain.
   
    public List<TValue> Range(TKey from, TKey to, out int nodesVisited)
    {
        var result = new List<TValue>();
        int visited = 0;
        Collect(_root, from, to, result, ref visited);
        nodesVisited = visited;
        return result;
    }

    //internals 

    private static int H(Node? n) => n == null ? 0 : n.Height;

    private static void Update(Node n) => n.Height = 1 + Math.Max(H(n.Left), H(n.Right));

    private static int Balance(Node n) => H(n.Left) - H(n.Right);

    private static Node RotateRight(Node y)
    {
        Node x = y.Left!;
        y.Left = x.Right;
        x.Right = y;
        Update(y);
        Update(x);
        return x;
    }

    private static Node RotateLeft(Node x)
    {
        Node y = x.Right!;
        x.Right = y.Left;
        y.Left = x;
        Update(x);
        Update(y);
        return y;
    }

    private static Node Insert(Node? node, TKey key, TValue value)
    {
        if (node == null) return new Node(key, value);

        int cmp = key.CompareTo(node.Key);
        if (cmp < 0) node.Left = Insert(node.Left, key, value);
        else if (cmp > 0) node.Right = Insert(node.Right, key, value);
        else
        {
            node.Values.Add(value);   // same key: no structural change, no rebalance needed
            return node;
        }

        Update(node);
        int balance = Balance(node);

        if (balance > 1)                                   // left-heavy
        {
            if (key.CompareTo(node.Left!.Key) > 0)         // left-right case
                node.Left = RotateLeft(node.Left);
            return RotateRight(node);
        }

        if (balance < -1)                                  // right-heavy
        {
            if (key.CompareTo(node.Right!.Key) < 0)        // right-left case
                node.Right = RotateRight(node.Right);
            return RotateLeft(node);
        }

        return node;
    }

    private static void Collect(Node? node, TKey from, TKey to, List<TValue> result, ref int visited)
    {
        if (node == null) return;
        visited++;

        int cmpFrom = from.CompareTo(node.Key);   // < 0 means node.Key is above 'from'
        int cmpTo = to.CompareTo(node.Key);       // > 0 means node.Key is below 'to'

        if (cmpFrom < 0) Collect(node.Left, from, to, result, ref visited);      // left subtree may hold matches
        if (cmpFrom <= 0 && cmpTo >= 0) result.AddRange(node.Values);            // this node is in range
        if (cmpTo > 0) Collect(node.Right, from, to, result, ref visited);       // right subtree may hold matches
    }
}
