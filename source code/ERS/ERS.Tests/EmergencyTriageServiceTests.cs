
using ERS.Core.Algorithms;
using ERS.Core.DataStructures;
using ERS.Core.Models;
using ERS.Core.Services;
using Xunit;

namespace ERS.Tests
{
    public class EmergencyTriageServiceTests
    {
        [Fact]
        public void TriageEmergency_ShouldCalculatePriorityAndEnqueue()
        {
            var queue = new EmergencyQueue();
            var service = new EmergencyTriageService(queue);

            var result = service.TriageEmergency(
                1, 101, 2, 30, 20,
                new DateTime(2026, 11, 17, 10, 0, 0));

            Assert.Equal(506, result.PriorityScore);
            Assert.Equal(1, queue.Count);
            Assert.Equal(1, queue.Peek().EmergencyId);
        }

        [Fact]
        public void TriageEmergency_ShouldPrioritizeHigherSeverity()
        {
            var queue = new EmergencyQueue();
            var service = new EmergencyTriageService(queue);

            service.TriageEmergency(1, 101, 3, 30, 20,
                new DateTime(2026, 11, 17, 10, 0, 0));

            service.TriageEmergency(2, 102, 1, 0, 0,
                new DateTime(2026, 11, 17, 10, 5, 0));

            Assert.Equal(2, queue.Peek().EmergencyId);
        }

        [Fact]
        public void TriageEmergency_ShouldPrioritizeEarlierReportWhenSeverityAndScoreEqual()
        {
            var queue = new EmergencyQueue();
            var service = new EmergencyTriageService(queue);

            service.TriageEmergency(1, 101, 2, 10, 10,
                new DateTime(2026, 11, 17, 10, 5, 0));

            service.TriageEmergency(2, 102, 2, 10, 10,
                new DateTime(2026, 11, 17, 10, 0, 0));

            Assert.Equal(2, queue.Peek().EmergencyId);
        }

        [Fact]
        public void TriageEmergency_ShouldRejectInvalidSeverity()
        {
            var queue = new EmergencyQueue();
            var service = new EmergencyTriageService(queue);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.TriageEmergency(1, 101, 6, 30, 20, DateTime.Now));

            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void TriageEmergency_ShouldRejectNegativeWaitingMinutes()
        {
            var queue = new EmergencyQueue();
            var service = new EmergencyTriageService(queue);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.TriageEmergency(1, 101, 2, -5, 20, DateTime.Now));

            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void TriageEmergency_ShouldRejectInvalidTypeScore()
        {
            var queue = new EmergencyQueue();
            var service = new EmergencyTriageService(queue);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.TriageEmergency(1, 101, 2, 30, 60, DateTime.Now));

            Assert.Equal(0, queue.Count);
        }

    }
}
