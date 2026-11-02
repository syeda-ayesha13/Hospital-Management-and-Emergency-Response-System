using ERS.Core.DataStructures;
using ERS.Core.Models;
using Xunit;

namespace ERS.Tests
{
    public class ErEntryTests
    {
        [Fact]
        public void OldLowPriority_BeatsNewHigherPriority()
        {
            var a = new ErEntry(1, 2, 0, 0);
            var b = new ErEntry(2, 4, 30, 1);

            Assert.True(a.CompareTo(b) > 0);

            var heap = new Heap<ErEntry>();
            heap.Insert(b);
            heap.Insert(a);

            Assert.Equal(1, heap.ExtractMax().PatientId);
        }

        [Fact]
        public void SameArrivalMinute_HigherTriageWins()
        {
            var low = new ErEntry(1, 2, 10, 0);
            var high = new ErEntry(2, 5, 10, 1);

            Assert.True(high.CompareTo(low) > 0);
        }

        [Fact]
        public void EqualKeys_EarlierArrivalWins()
        {
            var first = new ErEntry(1, 3, 0, 0);    // key 3.0
            var second = new ErEntry(2, 4, 10, 1); // key 4 - 1.0 = 3.0

            Assert.True(first.CompareTo(second) > 0);
        }
    }
}