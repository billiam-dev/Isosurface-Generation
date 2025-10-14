using System.Collections;
using System.Collections.Generic;

namespace IsosurfaceGeneration
{
    struct CellCorners<T> : IEnumerable<T>
    {
        public T corner1;
        public T corner2;
        public T corner3;
        public T corner4;
        public T corner5;
        public T corner6;
        public T corner7;
        public T corner8;

        public T this[int index]
        {
            get
            {
                return index switch
                {
                    0 => corner1,
                    1 => corner2,
                    2 => corner3,
                    3 => corner4,
                    4 => corner5,
                    5 => corner6,
                    6 => corner7,
                    7 => corner8,
                    _ => throw new System.IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        corner1 = value;
                        break;
                    case 1:
                        corner2 = value;
                        break;
                    case 2:
                        corner3 = value;
                        break;
                    case 3:
                        corner4 = value;
                        break;
                    case 4:
                        corner5 = value;
                        break;
                    case 5:
                        corner6 = value;
                        break;
                    case 6:
                        corner7 = value;
                        break;
                    case 7:
                        corner8 = value;
                        break;
                    default: throw new System.IndexOutOfRangeException();
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
