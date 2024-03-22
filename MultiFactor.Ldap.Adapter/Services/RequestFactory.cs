using MultiFactor.Ldap.Adapter.Core;

namespace MultiFactor.Ldap.Adapter.Services
{
    /// <summary>
    /// The request factory with message ID counter.
    /// Before each creation of the request the counter is incremented.
    /// </summary>
    public class RequestFactory
    {
        //must not repeat proxied messages ids
        private int _messageId = int.MaxValue - 9999;

        public LdapPacket CreateRootDSERequest()
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, string.Empty));      //base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));            //scope: base
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));            //aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)1));               //size limit: 1
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));              //time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));                 //typesOnly: false

            searchRequest.ChildAttributes.Add(new LdapAttribute(7, "objectClass")); //filter

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);

            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "defaultNamingContext"));

            searchRequest.ChildAttributes.Add(attrList);

            return packet;
        }

        public LdapPacket CreateLoadProfileRequest(string userName, string baseDn)
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn));    //base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    //scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));    //aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)255));     //size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      //time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));         //typesOnly: false

            var identityType = IdentityTypeParser.Parse(userName);

            var and = new LdapAttribute((byte)LdapFilterChoice.and);

            var eq1 = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, identityType.ToString()));
            eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, userName));

            var eq2 = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            eq2.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "objectClass"));
            eq2.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "user"));

            and.ChildAttributes.Add(eq1);
            and.ChildAttributes.Add(eq2);

            searchRequest.ChildAttributes.Add(and);

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "uid"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "sAMAccountName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "UserPrincipalName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "DisplayName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "mail"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "memberOf"));

            searchRequest.ChildAttributes.Add(attrList);

            return packet;
        }

        public LdapPacket CreateMemberOfRequest(string userName)
        {
            var packet = new LdapPacket(_messageId++);

            var baseDn = LdapProfile.GetBaseDn(userName);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn));    //base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    //scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));    //aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)255));     //size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      //time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, true));          //typesOnly: true

            var filter = new LdapAttribute(9);

            filter.ChildAttributes.Add(new LdapAttribute(1, "1.2.840.113556.1.4.1941"));    //AD filter
            filter.ChildAttributes.Add(new LdapAttribute(2, "member"));
            filter.ChildAttributes.Add(new LdapAttribute(3, userName));
            filter.ChildAttributes.Add(new LdapAttribute(4, (byte)0));

            searchRequest.ChildAttributes.Add(filter);

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "distinguishedName"));

            searchRequest.ChildAttributes.Add(attrList);

            return packet;
        }
    }
}
