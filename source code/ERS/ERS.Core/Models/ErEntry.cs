namespace ERS.Core.Models
{
    public class ErEntry : IComparable<ErEntry>
    {
        public const double AgingRatePerMinute = 0.1;

        public Patient Patient { get; }
        public int TriageLevel { get; }
        public int ArrivalMinute { get; }
        public long ArrivalOrder { get; }
        public double AgingKey { get; }

        public ErEntry(Patient patient, int triageLevel, int arrivalMinute, long arrivalOrder)
        {
            Patient = patient;
            TriageLevel = triageLevel;
            ArrivalMinute = arrivalMinute;
            ArrivalOrder = arrivalOrder;
            AgingKey = triageLevel - AgingRatePerMinute * arrivalMinute;
        }

        public int CompareTo(ErEntry? other)
        {
            if (other is null) return 1;

            int byKey = AgingKey.CompareTo(other.AgingKey);
            if (byKey != 0) return byKey;

            return other.ArrivalOrder.CompareTo(ArrivalOrder); // earlier arrival wins ties
        }
    }
}