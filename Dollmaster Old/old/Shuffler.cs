using System;
using UnityEngine;

namespace DeluxePlugin
{
    // Original Author: Macgruber
    // This version being maintained by VAMDeluxe

    // Loops through array and shuffles on every loop.
    // Does not return one entry twice directly after each other.
    public class Shuffler<T>
    {
        private T[] array;
        private int index;

        public T[] list
        {
            get{
                return array;
            }
        }

        public Shuffler(T[] array)
        {
            this.array = array;
            this.index = -1;
            Shuffle(true);
        }

        // Get next entry
        public T Next()
        {
            if (array.Length == 0)
                return default(T);

            ++index;
            if (index >= array.Length)
            {
                Shuffle(false);
                index = 0;
            }
            return array[index];
        }


        private void Shuffle(bool allowRepeat)
        {
            int n = array.Length;
            if (n <= 2)
                return;

            // Modified Fisher-Yates shuffle
            int r = UnityEngine.Random.Range(0, n - (allowRepeat ? 0 : 1));
            T t = array[r];
            array[r] = array[0];
            array[0] = t;
            for (int i = 1; i < n; ++i)
            {
                r = i + UnityEngine.Random.Range(0, n - i);
                t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
        }
    }
}
