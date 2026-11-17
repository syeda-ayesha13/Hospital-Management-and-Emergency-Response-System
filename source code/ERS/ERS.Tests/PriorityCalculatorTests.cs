
using System;
using ERS.Core.Algorithms;
using Xunit;

namespace ERS.Tests
{
    public class PriorityCalculatorTests
    {
        [Fact]
        public void Calculate_ShouldReturnCorrectPriorityScore()
        {
            int result = PriorityCalculator.Calculate(
                severityLevel: 2,
                waitingMinutes: 30,
                typeScore: 20);

            Assert.Equal(506, result);
        }

        [Fact]
        public void Calculate_HigherSeverity_ShouldHaveHigherScore()
        {
            int critical = PriorityCalculator.Calculate(1, 0, 0);
            int minor = PriorityCalculator.Calculate(5, 0, 0);

            Assert.True(critical > minor);
        }

        [Fact]
        public void Calculate_WaitingScore_ShouldBeCappedAt100()
        {
            int result = PriorityCalculator.Calculate(3, 1000, 0);

            Assert.Equal(460, result);
        }

        [Fact]
        public void Calculate_InvalidSeverity_ShouldThrowException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PriorityCalculator.Calculate(6, 10, 20));
        }

        [Fact]
        public void Calculate_NegativeWaitingTime_ShouldThrowException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PriorityCalculator.Calculate(2, -5, 20));
        }

        [Fact]
        public void Calculate_InvalidTypeScore_ShouldThrowException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PriorityCalculator.Calculate(2, 10, 60));
        }
    }
}
