
namespace ERS.Core.Algorithms
{
    public static class PriorityCalculator
    {
        public static int Calculate(
            int severityLevel,
            int waitingMinutes,
            int typeScore)
        {
            if (severityLevel < 1 || severityLevel > 5)
                throw new ArgumentOutOfRangeException(
                    nameof(severityLevel));

            if (waitingMinutes < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(waitingMinutes));

            if (typeScore < 0 || typeScore > 50)
                throw new ArgumentOutOfRangeException(
                    nameof(typeScore));

            int severityScore = (6 - severityLevel) * 120;

            int waitingScore = Math.Min(
                waitingMinutes / 5, 100);

            return severityScore + waitingScore + typeScore;
        }
    }
}
