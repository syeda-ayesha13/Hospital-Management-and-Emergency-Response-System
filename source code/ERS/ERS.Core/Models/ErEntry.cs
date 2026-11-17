namespace ERS.Core.Models
{
    public class ErEntry : IComparable<ErEntry>
    {
        public int EmergencyId { get; }
        public int PatientId { get; }
        public int PriorityScore { get; }
        public int SeverityLevel { get; }
        public DateTime ReportedAt { get; }

        public ErEntry(
            int emergencyId,
            int patientId,
            int priorityScore,
            int severityLevel,
            DateTime reportedAt)
        {
            if (severityLevel < 1 || severityLevel > 5)
                throw new ArgumentOutOfRangeException(nameof(severityLevel));

            EmergencyId = emergencyId;
            PatientId = patientId;
            PriorityScore = priorityScore;
            SeverityLevel = severityLevel;
            ReportedAt = reportedAt;
        }


        public int CompareTo(ErEntry? other)
        {
            if (other is null) return 1;

            // Lower severity number gets higher priority
            int bySeverity = other.SeverityLevel.CompareTo(SeverityLevel);
            if (bySeverity != 0) return bySeverity;

            // Higher priority score wins
            int byPriority = PriorityScore.CompareTo(other.PriorityScore);
            if (byPriority != 0) return byPriority;

            // Earlier reported emergency wins
            return other.ReportedAt.CompareTo(ReportedAt);
        }

    }
}