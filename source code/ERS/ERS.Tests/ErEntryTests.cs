using ERS.Core.DataStructures;
using ERS.Core.Models;
using Xunit;

namespace ERS.Tests
{
    public class ErEntryTests
    {
        private static Patient P(int id, string name) =>
            new Patient(id, name, 0, DateTime.MinValue, 0);

        [Fact]
        public void OldLowPriority_BeatsNewHigherPriority()
        {
            var a = new ErEntry(P(1, "A"), triageLevel: 2, arrivalMinute: 0, arrivalOrder: 0);
            var b = new ErEntry(P(2, "B"), triageLevel: 4, arrivalMinute: 30, arrivalOrder: 1);

            Assert.True(a.CompareTo(b) > 0);

            var heap = new Heap<ErEntry>();
            heap.Insert(b);
            heap.Insert(a);

            Assert.Equal("A", heap.ExtractMax().Patient.Name);
        }

        [Fact]
        public void SameArrivalMinute_HigherTriageWins()
        {
            var low = new ErEntry(P(1, "Low"), 2, 10, 0);
            var high = new ErEntry(P(2, "High"), 5, 10, 1);

            Assert.True(high.CompareTo(low) > 0);
        }

        [Fact]
        public void EqualKeys_EarlierArrivalWins()
        {
            var first = new ErEntry(P(1, "First"), 3, 0, 0);    // key 3.0
            var second = new ErEntry(P(2, "Second"), 4, 10, 1); // key 4 - 1.0 = 3.0

            Assert.True(first.CompareTo(second) > 0);
        }
    }
}