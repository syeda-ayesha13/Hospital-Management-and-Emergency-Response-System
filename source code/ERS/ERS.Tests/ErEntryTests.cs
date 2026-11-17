
using System;
using ERS.Core.Models;
using Xunit;

namespace ERS.Tests
{
    public class ErEntryTests
    {
        [Fact]
        public void Constructor_ShouldSetAllProperties()
        {
            var reportedAt = new DateTime(2026, 11, 16, 10, 0, 0);

            var entry = new ErEntry(
                101, 1, 90, 2, reportedAt);

            Assert.Equal(101, entry.EmergencyId);
            Assert.Equal(1, entry.PatientId);
            Assert.Equal(90, entry.PriorityScore);
            Assert.Equal(2, entry.SeverityLevel);
            Assert.Equal(reportedAt, entry.ReportedAt);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public void Constructor_InvalidSeverity_ShouldThrowException(int severity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ErEntry(101, 1, 90, severity, DateTime.Now));
        }

        [Fact]
        public void CompareTo_HigherPriority_ShouldReturnGreaterValue()
        {
            var e1 = new ErEntry(101, 1, 80, 3, DateTime.Now);
            var e2 = new ErEntry(102, 2, 95, 1, DateTime.Now);

            Assert.True(e2.CompareTo(e1) > 0);
        }

        [Fact]
        public void CompareTo_EarlierReportTime_ShouldReturnGreaterValue()
        {
            var earlier = new ErEntry(
                101, 1, 90, 2,
                new DateTime(2026, 11, 16, 9, 0, 0));

            var later = new ErEntry(
                102, 2, 90, 2,
                new DateTime(2026, 11, 16, 10, 0, 0));

            Assert.True(earlier.CompareTo(later) > 0);
        }

        [Fact]
        public void CompareTo_Null_ShouldReturnOne()
        {
            var entry = new ErEntry(
                101, 1, 90, 2, DateTime.Now);

            Assert.Equal(1, entry.CompareTo(null));
        }

        [Fact]
        public void CompareTo_EqualPriorityAndTime_ShouldReturnZero()
        {
            var time = new DateTime(2026, 11, 16, 10, 0, 0);

            var e1 = new ErEntry(101, 1, 90, 2, time);
            var e2 = new ErEntry(102, 2, 90, 2, time);

            Assert.Equal(0, e1.CompareTo(e2));
        }
    }
}
