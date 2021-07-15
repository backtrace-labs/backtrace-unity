using System;
using System.IO;

namespace Backtrace.Unity.Extensions
{
    internal static class StreamExtensions
    {
#if !(NET_STANDARD_2_0 && NET_4_6)
        // We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
        // improvement in Copy performance.
        private const int _DefaultCopyBufferSize = 81920;

        public static void CopyTo(this Stream original, Stream destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (!original.CanRead && !original.CanWrite)
            {
                throw new ObjectDisposedException("ObjectDisposedException");
            }
            if (!destination.CanRead && !destination.CanWrite)
            {
                throw new ObjectDisposedException("ObjectDisposedException");
            }
            if (!original.CanRead)
            {
                throw new NotSupportedException("NotSupportedException source");
            }
            if (!destination.CanWrite)
            {
                throw new NotSupportedException("NotSupportedException destination");
            }

            byte[] array = new byte[_DefaultCopyBufferSize];
            int count;
            while ((count = original.Read(array, 0, array.Length)) != 0)
            {
                destination.Write(array, 0, count);
            }
        }
#endif
    }
}
