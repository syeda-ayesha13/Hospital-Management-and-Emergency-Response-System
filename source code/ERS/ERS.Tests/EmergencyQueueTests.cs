
using ERS.Core.DataStructures;
using ERS.Core.Models;
using Xunit;

namespace ERS.Tests
{
    public class EmergencyQueueTests
    {
        [Fact]
        public void Dequeue_ShouldReturnHighestPriorityFirst()
        {
            var queue = new EmergencyQueue();

            var e1 = new ErEntry(101, 1, 90, 2,
                new DateTime(2026, 11, 16, 10, 0, 0));

            var e2 = new ErEntry(102, 2, 95, 1,
                new DateTime(2026, 11, 16, 10, 5, 0));

            var e3 = new ErEntry(103, 3, 90, 2,
                new DateTime(2026, 11, 16, 9, 50, 0));

            queue.Enqueue(e1);
            queue.Enqueue(e2);
            queue.Enqueue(e3);

            Assert.Equal(102, queue.Dequeue().EmergencyId);
            Assert.Equal(103, queue.Dequeue().EmergencyId);
            Assert.Equal(101, queue.Dequeue().EmergencyId);

            Assert.True(queue.IsEmpty);
        }


        [Fact]
        public void RemoveByEmergencyId_ShouldRemoveExistingEmergency()
        {
            var queue = new EmergencyQueue();

            queue.Enqueue(new ErEntry(1, 101, 500, 2, DateTime.Now));
            queue.Enqueue(new ErEntry(2, 102, 400, 3, DateTime.Now));

            bool removed = queue.RemoveByEmergencyId(1);

            Assert.True(removed);
            Assert.Equal(1, queue.Count);
            Assert.Equal(2, queue.Peek().EmergencyId);
        }

        [Fact]
        public void RemoveByEmergencyId_ShouldReturnFalseWhenNotFound()
        {
            var queue = new EmergencyQueue();

            queue.Enqueue(new ErEntry(1, 101, 500, 2, DateTime.Now));

            bool removed = queue.RemoveByEmergencyId(99);

            Assert.False(removed);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void RemoveByEmergencyId_ShouldPreservePriorityOrder()
        {
            var queue = new EmergencyQueue();

            queue.Enqueue(new ErEntry(1, 101, 500, 2, DateTime.Now));
            queue.Enqueue(new ErEntry(2, 102, 400, 3, DateTime.Now));
            queue.Enqueue(new ErEntry(3, 103, 600, 2, DateTime.Now));

            queue.RemoveByEmergencyId(2);

            Assert.Equal(3, queue.Dequeue().EmergencyId);
            Assert.Equal(1, queue.Dequeue().EmergencyId);
            Assert.True(queue.IsEmpty);
        }


        [Fact]
        public void Enqueue_ShouldRejectDuplicateEmergencyId()
        {
            var queue = new EmergencyQueue();

            queue.Enqueue(new ErEntry(1, 101, 500, 2, DateTime.Now));

            var duplicate = new ErEntry(1, 102, 600, 1, DateTime.Now);

            Assert.Throws<InvalidOperationException>(() =>
                queue.Enqueue(duplicate));

            Assert.Equal(1, queue.Count);
            Assert.Equal(1, queue.Peek().EmergencyId);
        }


        [Fact]
        public void Enqueue_ShouldAllowDifferentEmergencyIds()
        {
            var queue = new EmergencyQueue();

            queue.Enqueue(new ErEntry(1, 101, 500, 2, DateTime.Now));
            queue.Enqueue(new ErEntry(2, 102, 600, 1, DateTime.Now));

            Assert.Equal(2, queue.Count);
            Assert.Equal(2, queue.Peek().EmergencyId);
        }



    }
}
