using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace IsosurfaceGeneration
{
    public unsafe struct NativeCounter : IDisposable
    {
        readonly Allocator m_Allocator;

        [NativeDisableUnsafePtrRestriction] readonly int* m_Counter;

        public int Count
        {
            get => *m_Counter;
            set => (*m_Counter) = value;
        }

        public NativeCounter(Allocator allocator)
        {
            m_Allocator = allocator;
            m_Counter = (int*)UnsafeUtility.Malloc(sizeof(int), 4, allocator);
            Count = 0;
        }

        public int Increment()
        {
            return Interlocked.Increment(ref *m_Counter) - 1;
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Counter, m_Allocator);
        }
    }
}
