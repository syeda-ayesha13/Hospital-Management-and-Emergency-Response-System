
using ERS.Core.Models;

namespace ERS.Core.DataStructures
{
    public class EmergencyQueue
    {
        private readonly Heap<ErEntry> heap = new();

        public int Count => heap.Count;

        public bool IsEmpty => heap.IsEmpty;


        public void Enqueue(ErEntry emergency)
        {
            if (emergency is null)
                throw new ArgumentNullException(nameof(emergency));

            var items = new List<ErEntry>();

            while (!heap.IsEmpty)
            {
                var current = heap.ExtractMax();
                items.Add(current);

                if (current.EmergencyId == emergency.EmergencyId)
                {
                    foreach (var item in items)
                        heap.Insert(item);

                    throw new InvalidOperationException(
                        "Emergency already exists in the queue.");
                }
            }

            foreach (var item in items)
                heap.Insert(item);

            heap.Insert(emergency);
        }


        public ErEntry Peek()
        {
            return heap.Peek();
        }

        public ErEntry Dequeue()
        {
            return heap.ExtractMax();
        }
    


    
    public bool RemoveByEmergencyId(int emergencyId)
        {
            var items = new List<ErEntry>();

            while (!heap.IsEmpty)
            {
                var current = heap.ExtractMax();

                if (current.EmergencyId == emergencyId)
                {
                    foreach (var item in items)
                        heap.Insert(item);

                    return true;
                }

                items.Add(current);
            }

            foreach (var item in items)
                heap.Insert(item);

            return false;
        }
    }
}
