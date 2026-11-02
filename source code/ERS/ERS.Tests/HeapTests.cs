using System;
using ERS.Core.DataStructures;
using Xunit;

namespace ERS.Tests;

public class HeapTests
{
    // Test-only item. Independent of Patient, so Patient can change freely.
    private class Item : IComparable<Item>
    {
        public string Name { get; }
        public int Priority { get; }
        public long Order { get; }

        public Item(string name, int priority, long order)
        {
            Name = name;
            Priority = priority;
            Order = order;
        }

        public int CompareTo(Item? other)
        {
            if (other is null) return 1;

            int byPriority = Priority.CompareTo(other.Priority);
            if (byPriority != 0) return byPriority;

            return other.Order.CompareTo(Order); // earlier order wins ties
        }
    }

    [Fact]
    public void ExtractMax_ServesByPriority_ThenArrivalOrder()
    {
        var heap = new Heap<Item>();
        heap.Insert(new Item("A", 40, 0));
        heap.Insert(new Item("B", 90, 1));
        heap.Insert(new Item("C", 60, 2));
        heap.Insert(new Item("D", 90, 3));   // ties with B

        Assert.Equal("B", heap.ExtractMax().Name);
        Assert.Equal("D", heap.ExtractMax().Name);
        Assert.Equal("C", heap.ExtractMax().Name);
        Assert.Equal("A", heap.ExtractMax().Name);
    }

    [Fact]
    public void ExtractMax_OnEmptyHeap_Throws()
    {
        var heap = new Heap<Item>();
        Assert.Throws<InvalidOperationException>(() => heap.ExtractMax());
    }
}