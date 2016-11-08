using System;
using System.Collections.Concurrent;

namespace Mirai
{
    class BufferPool
    {
        internal int BufferSize
        {
            get;
            private set;
        }

        private int MaxQueued;
        private ConcurrentQueue<byte[]> Buffers = new ConcurrentQueue<byte[]>();

        internal BufferPool(int UseBufferSize, int UseMaxQueued)
        {
            BufferSize = UseBufferSize;
            MaxQueued = UseMaxQueued;
        }

        internal byte[] Take()
        {
            byte[] Return;
            if (!Buffers.TryDequeue(out Return))
            {
                Return = new byte[BufferSize];
            }

            return Return;
        }

        internal void Return(byte[] ToReturn)
        {
            if (ToReturn != null && ToReturn.Length == BufferSize && Buffers.Count < MaxQueued)
            {
                Array.Clear(ToReturn, 0, ToReturn.Length);
                Buffers.Enqueue(ToReturn);
            }
        }
    }
}
