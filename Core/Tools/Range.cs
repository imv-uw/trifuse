using System;
using System.Collections.Generic;

namespace Core.Utilities
{
    public struct Range
    {
        public int Start;
        public int End;

        public int Middle {  get { return (Start + End) / 2;  } }

        public Range(int start, int end)
        {
            if (end < start) throw new ArgumentException("Unexpected argument values: End < Start");

            Start = start;
            End = end;
        }

        public Range(Range other)
        {
            Start = other.Start;
            End = other.End;
        }

        public int GetRemappedIndex(int index, Range mappedRange)
        {
            return index + mappedRange.Start - Start;
        }

        public bool Contains(int index)
        {
            return Start <= index && index <= End;
        }

        public int Length { get { return End - Start + 1; } }

        public override string ToString() { return Start.ToString() + "-" + End.ToString(); }

        /// <summary>
        /// Finds the positions of a set of residues given an alignment offset provided in the form of two equivalent ranges
        /// </summary>
        /// <param name="remapped"></param>
        /// <param name="original">Pre-splice range of residues.</param>
        /// <param name="values"></param>
        /// <param name="checkBounds"></param>
        /// <returns></returns>
        public static List<int> GetRemappedRangeValues(Range remapped, Range original, List<int> values, bool checkBounds = true)
        {
            List<int> results = new List<int>(values.Count);
            int delta = remapped.Start - original.Start;
            foreach (int value in values)
            {
                if (checkBounds && (value < original.Start || original.End < value))
                    throw new ArgumentException("Values being remapped must fit in the original range");
                if (remapped.Length != original.Length)
                    throw new ArgumentException("Sequence range remapping requires ranges of the same length");
                results.Add(value + delta);
            }
            return results;
        }
    }
}
