namespace PacketParser
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public interface IPopularityList<TKey, TValue>
    {
        event PopularityList<TKey, TValue>.PopularityLostEventHandler PopularityLost;

        void Add(TKey key, TValue value);
        bool ContainsKey(TKey key);
        IEnumerable<TValue> GetValueEnumerator();
        void Remove(TKey key);

        int Count { get; }

        TValue this[TKey key] { get; set; }
    }
}

