using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Tests.TestDoubles;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Core
{
    public class LdapPacketTests
    {
        private static LdapPacket BuildSimpleBindRequest(int messageId, string userName, string password)
        {
            var packet = new LdapPacket(messageId);

            var bindRequest = new LdapAttribute(LdapOperation.BindRequest);
            bindRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, 3));
            bindRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, userName));
            bindRequest.ChildAttributes.Add(new LdapAttribute((byte)0, password));
            packet.ChildAttributes.Add(bindRequest);

            return packet;
        }

        [Fact]
        public async Task ParsePacket_FromBytes_RestoresASimpleBindRequest()
        {
            var bytes = BuildSimpleBindRequest(1, "user@domain.local", "P@ssw0rd").GetBytes();

            var parsed = await LdapPacket.ParsePacket(bytes);

            Assert.Equal(1, parsed.MessageId);

            var bindRequest = parsed.ChildAttributes.Single(a => a.LdapOperation == LdapOperation.BindRequest);
            Assert.Equal(3, bindRequest.ChildAttributes[0].GetValue<int>());
            Assert.Equal("user@domain.local", bindRequest.ChildAttributes[1].GetValue<string>());
            Assert.Equal("P@ssw0rd", bindRequest.ChildAttributes[2].GetValue<string>());
            Assert.Equal((byte)0, bindRequest.ChildAttributes[2].ContextType);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(16)]
        public async Task ParsePacket_FromStream_SurvivesSegmentedReads(int chunkSize)
        {
            var bytes = BuildSimpleBindRequest(9, "DOMAIN\\user", "P@ssw0rd").GetBytes();

            var parsed = await LdapPacket.ParsePacket(new ChunkedStream(bytes, chunkSize));

            Assert.Equal(9, parsed.MessageId);

            var bindRequest = parsed.ChildAttributes.Single(a => a.LdapOperation == LdapOperation.BindRequest);
            Assert.Equal("DOMAIN\\user", bindRequest.ChildAttributes[1].GetValue<string>());
            Assert.Equal("P@ssw0rd", bindRequest.ChildAttributes[2].GetValue<string>());
        }

        [Fact]
        public async Task ParsePacket_FromStream_LargePayloadSplitAcrossSegments()
        {
            var packet = new LdapPacket(11);
            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, new string('a', 5000)));
            packet.ChildAttributes.Add(searchRequest);

            var parsed = await LdapPacket.ParsePacket(new ChunkedStream(packet.GetBytes(), 512));

            var parsedSearch = parsed.ChildAttributes.Single(a => a.LdapOperation == LdapOperation.SearchRequest);
            Assert.Equal(new string('a', 5000), parsedSearch.ChildAttributes[0].GetValue<string>());
        }

        [Fact]
        public async Task ParsePacket_FromStream_TruncatedPayload_Throws()
        {
            var bytes = BuildSimpleBindRequest(1, "user@domain.local", "secret").GetBytes();
            var truncated = bytes.Take(bytes.Length - 5).ToArray();

            await Assert.ThrowsAsync<EndOfStreamException>(
                () => LdapPacket.ParsePacket(new MemoryStream(truncated)));
        }

        [Fact]
        public async Task ParsePacket_FromStream_EmptyStream_ReturnsNull()
        {
            Assert.Null(await LdapPacket.ParsePacket(new MemoryStream(new byte[0])));
        }

        [Fact]
        public async Task ParsePacket_FromStream_SearchResultDone_ReturnsNull()
        {
            var packet = new LdapPacket(3);
            var done = new LdapAttribute(LdapOperation.SearchResultDone);
            done.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)LdapResult.success));
            done.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, string.Empty));
            packet.ChildAttributes.Add(done);

            Assert.Null(await LdapPacket.ParsePacket(new MemoryStream(packet.GetBytes())));
        }

        [Fact]
        public async Task ParsePacket_FromBytes_DropsATrailingTwoByteAttribute()
        {
            var packet = new LdapPacket(1);
            packet.ChildAttributes.Add(new LdapAttribute(LdapOperation.UnbindRequest));
            var bytes = packet.GetBytes();

            var fromBytes = await LdapPacket.ParsePacket(bytes);
            var fromStream = await LdapPacket.ParsePacket(new MemoryStream(bytes));

            Assert.Single(fromBytes.ChildAttributes);
            Assert.Equal(2, fromStream.ChildAttributes.Count);
            Assert.Equal(LdapOperation.UnbindRequest, fromStream.ChildAttributes[1].LdapOperation);
        }

        [Fact]
        public async Task GetBytes_AfterParsing_ReproducesTheOriginalEncoding()
        {
            var original = BuildSimpleBindRequest(5, "user@domain.local", "secret").GetBytes();

            var parsed = await LdapPacket.ParsePacket(new MemoryStream(original));

            Assert.Equal(original, parsed.GetBytes());
        }
    }
}
