using System.IO;
using System.Threading.Tasks;
using MultiFactor.Ldap.Adapter.Core;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Core
{
    public class UtilsTests
    {
        [Theory]
        [InlineData(0, new byte[] { 0x00 })]
        [InlineData(127, new byte[] { 0x7F })]
        public void IntToBerLength_ShortNotation_EncodesLengthInASingleByte(int length, byte[] expected)
        {
            Assert.Equal(expected, Utils.IntToBerLength(length));
        }

        [Theory]
        [InlineData(128, new byte[] { 0x84, 0x00, 0x00, 0x00, 0x80 })]
        [InlineData(65536, new byte[] { 0x84, 0x00, 0x01, 0x00, 0x00 })]
        public void IntToBerLength_LongNotation_EncodesLengthBigEndian(int length, byte[] expected)
        {
            Assert.Equal(expected, Utils.IntToBerLength(length));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(65535)]
        public async Task BerLength_RoundTrip_FromByteArray(int length)
        {
            var encoded = Utils.IntToBerLength(length);

            var decoded = await Utils.BerLengthToInt(encoded, 0);

            Assert.Equal(length, decoded.Length);
            Assert.Equal(encoded.Length, decoded.BerByteCount);
        }

        [Theory]
        [InlineData(127)]
        [InlineData(70000)]
        public async Task BerLength_RoundTrip_FromStream(int length)
        {
            var encoded = Utils.IntToBerLength(length);

            var decoded = await Utils.BerLengthToInt(new MemoryStream(encoded));

            Assert.Equal(length, decoded.Length);
            Assert.Equal(encoded.Length, decoded.BerByteCount);
        }

        [Fact]
        public async Task BerLengthToInt_HonoursOffset()
        {
            var decoded = await Utils.BerLengthToInt(new byte[] { 0xFF, 0xFF, 0x7F }, 2);

            Assert.Equal(127, decoded.Length);
            Assert.Equal(1, decoded.BerByteCount);
        }

        [Fact]
        public async Task BerLengthToInt_MinimalLongNotation_IsAccepted()
        {
            var decoded = await Utils.BerLengthToInt(new byte[] { 0x81, 0x80 }, 0);

            Assert.Equal(128, decoded.Length);
            Assert.Equal(2, decoded.BerByteCount);
        }

        [Fact]
        public async Task BerLengthToInt_TruncatedLongNotation_Throws()
        {
            await Assert.ThrowsAsync<EndOfStreamException>(
                () => Utils.BerLengthToInt(new byte[] { 0x84, 0x00, 0x00 }, 0));
        }
    }
}
