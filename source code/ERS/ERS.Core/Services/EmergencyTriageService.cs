
using ERS.Core.Algorithms;
using ERS.Core.DataStructures;
using ERS.Core.Models;

namespace ERS.Core.Services
{
    public class EmergencyTriageService
    {
        private readonly EmergencyQueue _queue;

        public EmergencyTriageService(EmergencyQueue queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public ErEntry TriageEmergency(
            int emergencyId,
            int patientId,
            int severityLevel,
            int waitingMinutes,
            int typeScore,
            DateTime reportedAt)
        {
            int priorityScore = PriorityCalculator.Calculate(
                severityLevel,
                waitingMinutes,
                typeScore);

            var entry = new ErEntry(
                emergencyId,
                patientId,
                priorityScore,
                severityLevel,
                reportedAt);

            _queue.Enqueue(entry);

            return entry;
        }
    }
}
