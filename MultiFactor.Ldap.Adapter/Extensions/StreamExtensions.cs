using System.IO;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes into <paramref name="buffer"/> starting at
        /// <paramref name="offset"/>, looping until the buffer is filled. A single <see cref="Stream.ReadAsync(byte[], int, int)"/>
        /// may return fewer bytes than requested (e.g. a TCP segment boundary), so callers that need a
        /// fixed number of bytes must keep reading.
        /// Mirrors the behaviour of the built-in Stream.ReadExactlyAsync (.NET 7+), which is not available
        /// on .NET Framework: throws <see cref="EndOfStreamException"/> if the stream ends before
        /// <paramref name="count"/> bytes have been read.
        /// </summary>
        public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    throw new EndOfStreamException(
                        $"Unable to read {count} byte(s) from the stream: reached the end after {totalRead} byte(s)");
                }

                totalRead += read;
            }
        }
    }
}
