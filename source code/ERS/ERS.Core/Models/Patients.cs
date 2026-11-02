using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS.Core.Models
{
    public class Patient : IComparable<Patient>
    {
        public int Id { get; }
        public string Name { get; }
        public int Priority { get; }          // bigger number = more urgent
        public DateTime ArrivedAt { get; }
        public long ArrivalOrder { get; }     // 0, 1, 2... used to break ties

        public Patient(int id, string name, int priority, DateTime arrivedAt, long arrivalOrder)
        {
            Id = id;
            Name = name;
            Priority = priority;
            ArrivedAt = arrivedAt;
            ArrivalOrder = arrivalOrder;
        }

        // "Greater" means "should be served first".
        public int CompareTo(Patient? other)
        {
            if (other is null) return 1;

            int byPriority = Priority.CompareTo(other.Priority);
            if (byPriority != 0) return byPriority;

            // Same priority: the EARLIER arrival counts as greater,
            // so the comparison is deliberately flipped (other vs this).
            return other.ArrivalOrder.CompareTo(ArrivalOrder);
        }

        public override string ToString() => $"{Name} (priority {Priority}, #{ArrivalOrder})";
    }
}
