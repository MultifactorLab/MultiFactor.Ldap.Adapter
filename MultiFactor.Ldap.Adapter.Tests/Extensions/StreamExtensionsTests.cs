using System.IO;
using System.Text;
using System.Threading.Tasks;
using MultiFactor.Ldap.Adapter.Extensions;
using MultiFactor.Ldap.Adapter.Tests.TestDoubles;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Extensions
{
    public class StreamExtensionsTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public async Task ReadExactlyAsync_StreamReturnsDataInChunks_FillsWholeBuffer(int chunkSize)
        {
            var payload = Encoding.UTF8.GetBytes("the quick brown fox jumps over the lazy dog");
            var stream = new ChunkedStream(payload, chunkSize);
            var buffer = new byte[payload.Length];

            await stream.ReadExactlyAsync(buffer, 0, payload.Length);

            Assert.Equal(payload, buffer);
        }

        [Fact]
        public async Task ReadExactlyAsync_SingleByteChunks_KeepsReadingUntilCountIsSatisfied()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5 };
            var stream = new ChunkedStream(payload, 1);
            var buffer = new byte[payload.Length];

            await stream.ReadExactlyAsync(buffer, 0, payload.Length);

            Assert.Equal(payload, buffer);
            Assert.Equal(payload.Length, stream.ReadCallCount);
        }

        [Fact]
        public async Task ReadExactlyAsync_WithOffset_WritesAtTheRequestedOffsetOnly()
        {
            var stream = new ChunkedStream(new byte[] { 10, 20, 30 }, 1);
            var buffer = new byte[6];

            await stream.ReadExactlyAsync(buffer, 2, 3);

            Assert.Equal(new byte[] { 0, 0, 10, 20, 30, 0 }, buffer);
        }

        [Fact]
        public async Task ReadExactlyAsync_StreamEndsBeforeCount_ThrowsEndOfStream()
        {
            var stream = new ChunkedStream(new byte[] { 1, 2, 3 }, 2);

            var ex = await Assert.ThrowsAsync<EndOfStreamException>(
                () => stream.ReadExactlyAsync(new byte[10], 0, 10));

            Assert.Contains("10", ex.Message);
            Assert.Contains("3", ex.Message);
        }

        [Fact]
        public async Task ReadExactlyAsync_ZeroCount_DoesNotTouchTheStream()
        {
            var stream = new ChunkedStream(new byte[0], 1);

            await stream.ReadExactlyAsync(new byte[4], 0, 0);

            Assert.Equal(0, stream.ReadCallCount);
        }
    }
}
