using MultiFactor.Ldap.Adapter.Core;
using System.Text.RegularExpressions;
using System;

namespace MultiFactor.Ldap.Adapter.Services
{
    /// <summary>
    /// The request factory with message ID counter.
    /// Иefore each creation of the request the counter is incremented.
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
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "email"));
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

        public LdapPacket CreateGetPartitions(string baseDn)
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "CN=Partitions,CN=Configuration," + baseDn));    // base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    // scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)3));    // aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (Int32)1000));     // size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      // time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));         //typesOnly: false

            var and = new LdapAttribute((byte)LdapFilterChoice.and);

            var eq1 = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            var present = new LdapAttribute((byte)LdapFilterChoice.present, "netbiosname");

            and.ChildAttributes.Add(eq1);
            and.ChildAttributes.Add(present);

            searchRequest.ChildAttributes.Add(and);

            eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "objectcategory"));
            eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "crossref"));

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "netbiosname"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "dnsRoot"));
            searchRequest.ChildAttributes.Add(attrList);
            return packet;
        }


        public LdapPacket CreateResolveProfileRequest(string name, string baseDn)
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn));    // base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    // scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));    // aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (Int32)1000));     // size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      // time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));         //typesOnly: false

            var and = new LdapAttribute((byte)LdapFilterChoice.and);
            var or = new LdapAttribute((byte)LdapFilterChoice.or);

            var sAMAccountNameEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            sAMAccountNameEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "sAMAccountName"));
            var sAMAccountNameRegex = new Regex("@[^@]+$");
            var netbiosRegex = new Regex(@"[^.]+\\");
            sAMAccountNameEq.ChildAttributes.Add(
                new LdapAttribute(UniversalDataType.OctetString,
                        netbiosRegex.Replace(sAMAccountNameRegex.Replace(name, ""), ""))
            );

            var upnEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            upnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "UserPrincipalName"));
            upnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, name));


            var dnEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            dnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "distinguishedName"));
            dnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, name));


            or.ChildAttributes.Add(sAMAccountNameEq);
            or.ChildAttributes.Add(upnEq);
            or.ChildAttributes.Add(dnEq);

            var objectClassEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);

            objectClassEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "objectClass"));
            objectClassEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "user"));

            and.ChildAttributes.Add(objectClassEq);
            and.ChildAttributes.Add(or);

            searchRequest.ChildAttributes.Add(and);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "uid"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "sAMAccountName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "UserPrincipalName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "DisplayName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "mail"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "memberOf"));

            searchRequest.ChildAttributes.Add(attrList);

            packet.ChildAttributes.Add(searchRequest);

            return packet;
        }
    }
}
