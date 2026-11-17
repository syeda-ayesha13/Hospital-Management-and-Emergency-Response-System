
using ERS.Core.DataStructures;
using Xunit;

namespace ERS.Tests
{
    public class HeapTests
    {
        [Fact]
        public void Insert_ShouldMaintainMaxHeapProperty()
        {
            var heap = new Heap<int>();

            heap.Insert(30);
            heap.Insert(50);
            heap.Insert(20);
            heap.Insert(70);

            Assert.Equal(70, heap.Peek());
        }

        [Fact]
        public void ExtractMax_ShouldReturnElementsInDescendingOrder()
        {
            var heap = new Heap<int>();

            heap.Insert(30);
            heap.Insert(50);
            heap.Insert(20);
            heap.Insert(70);

            Assert.Equal(70, heap.ExtractMax());
            Assert.Equal(50, heap.ExtractMax());
            Assert.Equal(30, heap.ExtractMax());
            Assert.Equal(20, heap.ExtractMax());
        }

        [Fact]
        public void Count_ShouldUpdateAfterInsertAndExtract()
        {
            var heap = new Heap<int>();

            heap.Insert(10);
            heap.Insert(20);

            Assert.Equal(2, heap.Count);

            heap.ExtractMax();

            Assert.Equal(1, heap.Count);
        }

        [Fact]
        public void IsEmpty_ShouldReturnCorrectValue()
        {
            var heap = new Heap<int>();

            Assert.True(heap.IsEmpty);

            heap.Insert(10);

            Assert.False(heap.IsEmpty);

            heap.ExtractMax();

            Assert.True(heap.IsEmpty);
        }

        [Fact]
        public void Peek_ShouldNotRemoveElement()
        {
            var heap = new Heap<int>();

            heap.Insert(40);
            heap.Insert(60);

            Assert.Equal(60, heap.Peek());
            Assert.Equal(2, heap.Count);
        }

        [Fact]
        public void ExtractMax_OnEmptyHeap_ShouldThrowException()
        {
            var heap = new Heap<int>();

            Assert.Throws<InvalidOperationException>(
                () => heap.ExtractMax());
        }

        [Fact]
        public void Peek_OnEmptyHeap_ShouldThrowException()
        {
            var heap = new Heap<int>();

            Assert.Throws<InvalidOperationException>(
                () => heap.Peek());
        }
    }
}
