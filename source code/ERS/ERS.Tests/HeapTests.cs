using System;
using ERS.Core.DataStructures;
using ERS.Core.Models;
using Xunit;

namespace ERS.Tests;

public class HeapTests
{
    private static Patient P(int id, string name, int priority, long order)
        => new Patient(id, name, priority, DateTime.Now, order);

    [Fact]
    public void ExtractMax_ServesByPriority_ThenArrivalOrder()
    {
        var heap = new Heap<Patient>();
        heap.Insert(P(1, "A", 40, 0));
        heap.Insert(P(2, "B", 90, 1));
        heap.Insert(P(3, "C", 60, 2));
        heap.Insert(P(4, "D", 90, 3));   // ties with B

        Assert.Equal("B", heap.ExtractMax().Name);
        Assert.Equal("D", heap.ExtractMax().Name);
        Assert.Equal("C", heap.ExtractMax().Name);
        Assert.Equal("A", heap.ExtractMax().Name);
    }

    [Fact]
    public void ExtractMax_OnEmptyHeap_Throws()
    {
        var heap = new Heap<Patient>();
        Assert.Throws<InvalidOperationException>(() => heap.ExtractMax());
    }
}
