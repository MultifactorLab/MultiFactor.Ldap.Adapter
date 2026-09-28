using System;
using MultiFactor.Ldap.Adapter.Core;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Core
{
    public class LdapAttributeTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(127)]
        [InlineData(65536)]
        [InlineData(int.MaxValue)]
        public void Integer_RoundTripsThroughGetValue(int value)
        {
            Assert.Equal(value, new LdapAttribute(UniversalDataType.Integer, value).GetValue<int>());
        }

        [Fact]
        public void Integer_IsEncodedBigEndian()
        {
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 },
                new LdapAttribute(UniversalDataType.Integer, 1).Value);
        }

        [Fact]
        public void OctetString_IsUtf8Encoded()
        {
            var attr = new LdapAttribute(UniversalDataType.OctetString, "Пользователь");

            Assert.Equal("Пользователь", attr.GetValue<string>());
            Assert.Equal(24, attr.Value.Length);
        }

        [Theory]
        [InlineData((byte)0, LdapResult.success)]
        [InlineData((byte)49, LdapResult.invalidCredentials)]
        public void Enumerated_IsReadAsLdapResult(byte raw, LdapResult expected)
        {
            Assert.Equal(expected, new LdapAttribute(UniversalDataType.Enumerated, raw).GetValue());
        }

        [Fact]
        public void UnsupportedValueType_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => new LdapAttribute(UniversalDataType.Integer, 1.5d));
        }

        [Fact]
        public void GetBytes_LeafAttribute_IsTagLengthValue()
        {
            Assert.Equal(new byte[] { 0x04, 0x03, (byte)'a', (byte)'b', (byte)'c' },
                new LdapAttribute(UniversalDataType.OctetString, "abc").GetBytes());
        }

        [Fact]
        public void GetBytes_WithChildren_MarksTheTagConstructedAndIgnoresValue()
        {
            var attr = new LdapAttribute(UniversalDataType.Sequence, "ignored");
            attr.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, 1));

            var bytes = attr.GetBytes();

            Assert.True(attr.IsConstructed);
            Assert.Equal(new byte[] { 0x30, 0x06, 0x02, 0x04, 0x00, 0x00, 0x00, 0x01 }, bytes);
        }

        [Fact]
        public void GetBytes_LongContent_UsesLongFormLength()
        {
            var bytes = new LdapAttribute(UniversalDataType.OctetString, new string('x', 200)).GetBytes();

            Assert.Equal(0x84, bytes[1]);
            Assert.Equal(206, bytes.Length);
        }
    }
}
