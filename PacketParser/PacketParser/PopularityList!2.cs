namespace PacketParser
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class PopularityList<TKey, TValue> : IPopularityList<TKey, TValue>
    {
        public delegate bool ListCanExpand(PopularityList<TKey, TValue> list);
        public delegate void PopularityLostEventHandler(TKey key, TValue value);

        private int currentPoolSize;
        private LinkedList<KeyValuePair<TKey, TValue>> linkedList;
        private ListCanExpand listCanExpandDelegate;

        private int maxPoolSize;
        private int minPoolSize;
        private SortedList<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> sortedList;

        public event PopularityLostEventHandler PopularityLost;

        public PopularityList(int maxPoolSize) : this(maxPoolSize, maxPoolSize, null)
        {
        }

        public PopularityList(int minPoolSize, int maxPoolSize, ListCanExpand listCanExpandDelegate)
        {
            this.minPoolSize = minPoolSize;
            this.maxPoolSize = maxPoolSize;
            this.currentPoolSize = this.minPoolSize;
            this.listCanExpandDelegate = listCanExpandDelegate;
            this.sortedList = new SortedList<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            this.linkedList = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public void Add(TKey key, TValue value)
        {
            KeyValuePair<TKey, TValue> pair = new KeyValuePair<TKey, TValue>(key, value);
            LinkedListNode<KeyValuePair<TKey, TValue>> node = new LinkedListNode<KeyValuePair<TKey, TValue>>(pair);
            if (this.sortedList.ContainsKey(key))
            {
                this.Remove(key);
            }
            this.linkedList.AddFirst(node);
            this.sortedList.Add(key, node);
            while (this.sortedList.Count > this.currentPoolSize)
            {
                if ((this.currentPoolSize < this.maxPoolSize) && this.listCanExpandDelegate((PopularityList<TKey, TValue>) this))
                {
                    this.currentPoolSize = Math.Min(this.sortedList.Count, this.maxPoolSize);
                }
                else
                {
                    LinkedListNode<KeyValuePair<TKey, TValue>> last = this.linkedList.Last;
                    this.sortedList.Remove(last.Value.Key);
                    this.linkedList.Remove(last);
                    if (this.PopularityLost != null)
                    {
                        this.PopularityLost(last.Value.Key, last.Value.Value);
                    }
                }
            }
        }

        public void Clear()
        {
            this.linkedList.Clear();
            this.sortedList.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return this.sortedList.ContainsKey(key);
        }

        public IEnumerable<TValue> GetValueEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> iteratorVariable0 in this.linkedList)
            {
                yield return iteratorVariable0.Value;
            }
        }

        public void Remove(TKey key)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node = this.sortedList[key];
            this.linkedList.Remove(node);
            this.sortedList.Remove(key);
        }

        public int Count
        {
            get
            {
                return this.sortedList.Count;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> node = this.sortedList[key];
                this.linkedList.Remove(node);
                this.linkedList.AddFirst(node);
                return node.Value.Value;
            }
            set
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> node = this.sortedList[key];
                this.linkedList.Remove(node);
                this.linkedList.AddFirst(node);
                node.Value = new KeyValuePair<TKey, TValue>(key, value);
            }
        }
    }
}

