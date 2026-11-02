using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERS.Core.DataStructures
{
    public class Heap<T> where T : IComparable<T>
    {
        private readonly List<T> items = new();

        public int Count => items.Count;
        public bool IsEmpty => items.Count == 0;

        private static int Parent(int i) => (i - 1) / 2;
        private static int Left(int i) => 2 * i + 1;
        private static int Right(int i) => 2 * i + 2;

        private void Swap(int a, int b)
        {
            T temp = items[a];
            items[a] = items[b];
            items[b] = temp;
        }

        public void Insert(T item)
        {
            items.Add(item);
            BubbleUp(items.Count - 1);
        }

        public T Peek()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Heap is empty.");
            return items[0];
        }

        public T ExtractMax()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Heap is empty.");

            T top = items[0];
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);

            if (items.Count > 0)
                SiftDown(0);

            return top;
        }

        private void BubbleUp(int i)
        {
            while (i > 0 && items[i].CompareTo(items[Parent(i)]) > 0)
            {
                Swap(i, Parent(i));
                i = Parent(i);
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int largest = i;
                int l = Left(i);
                int r = Right(i);

                if (l < items.Count && items[l].CompareTo(items[largest]) > 0) largest = l;
                if (r < items.Count && items[r].CompareTo(items[largest]) > 0) largest = r;

                if (largest == i) return;

                Swap(i, largest);
                i = largest;
            }
        }
    }

}
