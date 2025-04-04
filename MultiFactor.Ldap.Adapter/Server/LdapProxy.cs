﻿//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Server.Authentication;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapProxy
    {
        private TcpClient _clientConnection;
        private TcpClient _serverConnection;
        private Stream _clientStream;
        private Stream _serverStream;
        private ClientConfiguration _clientConfig;
        private readonly MultiFactorApiClient _apiClient;
        private ILogger _logger;
        private string _userName;
        private string _lookupUserName;

        private LdapService _ldapService;

        private LdapProxyAuthenticationStatus _status;

        private static readonly ConcurrentDictionary<string, string> _usersDn2Cn = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> _usersCn2Dn = new ConcurrentDictionary<string, string>();

        private readonly RandomWaiter _waiter;
        private NameResolverService _nameResolverService;

        public LdapProxy(TcpClient clientConnection, Stream clientStream, TcpClient serverConnection, 
            Stream serverStream, ClientConfiguration clientConfig,
            RandomWaiter waiter,
            MultiFactorApiClient apiClient,
            ILogger logger,
            NameResolverService nameResolverService)
        {
            _clientConnection = clientConnection ?? throw new ArgumentNullException(nameof(clientConnection));
            _clientStream = clientStream ?? throw new ArgumentNullException(nameof(clientStream));
            _serverConnection = serverConnection ?? throw new ArgumentNullException(nameof(serverConnection));
            _serverStream = serverStream ?? throw new ArgumentNullException(nameof(serverStream));

            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
            _waiter = waiter ?? throw new ArgumentNullException(nameof(waiter));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _ldapService = new LdapService();
            _nameResolverService = nameResolverService;
        }

        public async Task ProcessDataExchange()
        {
            var from = _clientConnection.Client.RemoteEndPoint.ToString();
            var to = _serverConnection.Client.RemoteEndPoint.ToString();

            _logger.Information("Opened {client} => {server} client {clientName:l}", from, to, _clientConfig.Name);

            await Task.WhenAny(
                DataExchange(_clientConnection, _clientStream, _serverConnection, _serverStream, ParseAndProcessRequest),
                DataExchange(_serverConnection, _serverStream, _clientConnection, _clientStream, ParseAndProcessResponse));

            _logger.Debug("Closed {client} => {server} client {clientName:l}", from, to, _clientConfig.Name);
        }

        private async Task DataExchange(TcpClient source, Stream sourceStream, TcpClient target, Stream targetStream, Func<byte[], int, Task<(byte[], int)>> process)
        {
            try
            {
                var bytesRead = 0;
                var requestData = new byte[8192];   //enough for bind request/result

                do
                {
                    //read
                    bytesRead = await sourceStream.ReadAsync(requestData, 0, requestData.Length);

                    //process
                    var response = await process(requestData, bytesRead);

                    //write
                    await targetStream.WriteAsync(response.Item1, 0, response.Item2);

                    if (_status == LdapProxyAuthenticationStatus.AuthenticationFailed)
                    {
                        source.Close();
                    }

                } while (bytesRead != 0);
            }
            catch (IOException)
            {
                //connection closed unexpectly
                //_logger.Debug(ioex, "proxy");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Data exchange error from {client} to {server}", source.Client.RemoteEndPoint, target.Client.RemoteEndPoint);
            }
        }

        private async Task<(byte[], int)> ParseAndProcessRequest(byte[] data, int length)
        {
            var packet = await LdapPacket.ParsePacket(data);

            var searchRequest = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchRequest);
            if (searchRequest != null)
            {
                var filter = searchRequest.ChildAttributes[6];
                var user = SearchUserName(filter);

                if (!string.IsNullOrEmpty(user))
                {
                    _status = LdapProxyAuthenticationStatus.UserDnSearch;
                    _lookupUserName = user;
                }
            }

            var bindRequest = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.BindRequest);
            if (bindRequest != null)
            {
                var authentication = LoadAuthentication(bindRequest);
                if (authentication != null && authentication.TryParse(bindRequest, out var userName))
                {
                    if (!string.IsNullOrEmpty(userName)) //empty userName means anonymous bind
                    {
                        if (IsServiceAccount(userName))
                        {
                            //service acc
                            _logger.Debug($"Received {authentication.MechanismName} bind request for service account '{{user:l}}' from {{client}} {{clientName:l}}", userName, _clientConnection.Client.RemoteEndPoint, _clientConfig.Name);
                        }
                        else
                        {
                            //user acc
                            _userName = ConvertDistinguishedNameToCommonName(userName);
                            _status = LdapProxyAuthenticationStatus.BindRequested;
                            _logger.Information($"Received {authentication.MechanismName} bind request for user '{{user:l}}' from {{client}} {{clientName:l}}", userName, _clientConnection.Client.RemoteEndPoint, _clientConfig.Name);
                        }
                    }
                }
            }

            return await Task.FromResult((data, length));
        }

        private async Task<(byte[], int)> ParseAndProcessResponse(byte[] data, int length)
        {
            if (_status == LdapProxyAuthenticationStatus.BindRequested)
            {
                var packet = await LdapPacket.ParsePacket(data);
                var bindResponse = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.BindResponse);

                if (bindResponse != null)
                {
                    var bindResult = bindResponse.ChildAttributes[0].GetValue<LdapResult>();
                    if (bindResult == LdapResult.saslBindInProgress)    //  challenge/response in process
                    {
                        return (data, length);  //just proxy
                    }

                    var bound = bindResult == LdapResult.success;

                    if (bound)  //first factor authenticated
                    {
                        var bypass = false;

                        //apply login transformation rules if any
                        _userName = ProcessUserNameTransformRules();

                        string baseDn = await _ldapService.GetBaseDn(_serverStream, _userName);

                        if (_clientConfig.TransformLdapIdentity != LdapIdentityFormat.None)
                        {
                            _userName = await EnforceLdapIdentityFormat(baseDn, _clientConfig.TransformLdapIdentity);
                        }

                        var profile = await _ldapService.LoadProfile(_serverStream, baseDn, _userName);
                        if (profile is null)
                        {
                            _logger.Error("User '{user:l}' not found. This is an unusual situation. Check the adapter settings", _userName);
                            _status = LdapProxyAuthenticationStatus.AuthenticationFailed;
                            var responsePacket = ProfileNotFound(packet);
                            var response = responsePacket.GetBytes();
                            return (response, response.Length);
                        }
                        
                        if (_clientConfig.CheckUserGroups())
                        {
                            profile.MemberOf = await _ldapService.GetAllGroups(_serverStream, profile, _clientConfig);

                            //check ACL
                            if (_clientConfig.ActiveDirectoryGroup.Any())
                            {
                                var accessGroup = _clientConfig.ActiveDirectoryGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                                if (accessGroup != null)
                                {
                                    _logger.Debug($"User '{{user:l}}' is member of '{accessGroup.Trim()}' access group in {profile.BaseDn}", _userName);
                                }
                                else
                                {
                                    _logger.Warning($"User '{{user:l}}' is not member of '{string.Join(";", _clientConfig.ActiveDirectoryGroup)}' access group in {profile.BaseDn}", _userName);

                                    //return invalid creds response
                                    var responsePacket = InvalidCredentials(packet);
                                    await _waiter.WaitSomeTimeAsync();
                                    var response = responsePacket.GetBytes();

                                    _logger.Debug("Sent invalid credential response for user '{user:l}' to {client}", _userName, _clientConnection.Client.RemoteEndPoint);

                                    _status = LdapProxyAuthenticationStatus.AuthenticationFailed;

                                    return (response, response.Length);
                                }
                            }

                            //check if mfa is mandatory
                            if (_clientConfig.ActiveDirectory2FaGroup.Any())
                            {
                                var mfaGroup = _clientConfig.ActiveDirectory2FaGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                                if (mfaGroup != null)
                                {
                                    _logger.Debug($"User '{{user:l}}' is member of '{mfaGroup.Trim()}' 2FA group in {profile.BaseDn}", _userName);

                                }
                                else
                                {
                                    _logger.Debug($"User '{{user:l}}' is not member of '{string.Join(";", _clientConfig.ActiveDirectory2FaGroup)}' 2FA group in {profile.BaseDn}", _userName);
                                    bypass = true;
                                }
                            }

                            //check of mfa is not mandatory
                            if (_clientConfig.ActiveDirectory2FaBypassGroup.Any() && !bypass)
                            {
                                var bypassGroup = _clientConfig.ActiveDirectory2FaBypassGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                                if (bypassGroup != null)
                                {
                                    _logger.Information($"User '{{user:l}}' is member of '{bypassGroup.Trim()}' 2FA bypass group in {profile.BaseDn}", _userName);
                                    bypass = true;
                                }
                                else
                                {
                                    _logger.Debug($"User '{{user:l}}' is not member of '{string.Join(";", _clientConfig.ActiveDirectory2FaBypassGroup)}' 2FA bypass group in {profile.BaseDn}", _userName);
                                }
                            }
                        }

                        if (!bypass)
                        {
                            if (IdentityTypeParser.Parse(_userName) == IdentityType.DistinguishedName)   //user uses DN as login ;)
                            {
                                _userName = profile?.Uid ?? _userName;
                            }

                            var personalData = new PersonalData(profile, _clientConfig.PrivacyModeDescriptor);
                            var result = await _apiClient.Authenticate(new ConnectedClientInfo(_userName, _clientConfig), personalData); //second factor

                            if (!result) // second factor failed
                            {
                                //return invalid creds response
                                var responsePacket = InvalidCredentials(packet);
                                await _waiter.WaitSomeTimeAsync();
                                var response = responsePacket.GetBytes();

                                _logger.Debug("Sent invalid credential response for user '{user:l}' to {client}", _userName, _clientConnection.Client.RemoteEndPoint);

                                _status = LdapProxyAuthenticationStatus.AuthenticationFailed;

                                return (response, response.Length);
                            }
                        }
                        else
                        {
                            _logger.Information("Bypass second factor for user '{user:l}'", _userName);
                        }

                        _status = LdapProxyAuthenticationStatus.None;
                    }
                    else //first factor authentication failed
                    {
                        //just log
                        var reason = bindResponse.ChildAttributes[2].GetValue<string>();
                        await _waiter.WaitSomeTimeAsync();
                        _logger.Warning("Verification user '{user:l}' at {server} failed: {reason}", _userName, _serverConnection.Client.RemoteEndPoint, reason);
                    }
                }
            }

            if (_status == LdapProxyAuthenticationStatus.UserDnSearch)
            {
                var packet = await LdapPacket.ParsePacket(data);
                var searchResultEntry = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);

                if (searchResultEntry != null)
                {
                    var userDn = searchResultEntry.ChildAttributes[0].GetValue<string>();

                    if (_lookupUserName != null && userDn != null)
                    {
                        userDn = userDn.ToLower(); //becouse some apps do it

                        _usersDn2Cn.TryRemove(userDn, out _);
                        _usersDn2Cn.TryAdd(userDn, _lookupUserName);

                        _usersCn2Dn.TryRemove(_lookupUserName, out _);
                        _usersCn2Dn.TryAdd(_lookupUserName, userDn);
                    }
                }

                _status = LdapProxyAuthenticationStatus.None;
            }

            return (data, length);  //just proxy
        }

        private string ConvertDistinguishedNameToCommonName(string dn)
        {
            if (string.IsNullOrEmpty(dn)) return dn;

            dn = dn.ToLower();

            if (_usersDn2Cn.TryGetValue(dn, out var cn))
            {
                return cn;
            }

            return dn;
        }

        private BindAuthentication LoadAuthentication(LdapAttribute bindRequest)
        {
            var authPacket = bindRequest.ChildAttributes[2];
            if (!authPacket.IsConstructed)  //simple bind or NTLM
            {
                if (BindAuthentication.IsNtlm(authPacket.Value))
                {
                    return new NtlmBindAuthentication(_logger);
                }

                return new SimpleBindAuthentication(_logger);
            }

            var mechanism = authPacket.ChildAttributes[0].GetValue<string>();

            if (mechanism == "DIGEST-MD5")
            {
                return new DigestMd5BindAuthentication(_logger);
            }

            if (mechanism == "GSS-SPNEGO")
            {
                var saslPacket = authPacket.ChildAttributes[1].Value;
                if (BindAuthentication.IsNtlm(saslPacket))
                {
                    return new SpnegoNtlmBindAuthentication(_logger);
                }
            }


            //kerberos or not-implemented
            //_logger.Debug($"Unknown bind mechanism: {mechanism}");

            return null;
        }

        private string SearchUserName(LdapAttribute attr)
        {
            var userNameAttrs = new[] { "cn", "uid", "samaccountname", "userprincipalname" };
            var contextType = (LdapFilterChoice)attr.ContextType;

            if (contextType == LdapFilterChoice.equalityMatch)
            {
                var left = attr.ChildAttributes[0].GetValue<string>()?.ToLower();
                var right = attr.ChildAttributes[1].GetValue<string>();

                if (userNameAttrs.Any(cn => cn == left))
                {
                    //user name lookup, from login to DN
                    return right;
                }
            }
            if (contextType == LdapFilterChoice.and || contextType == LdapFilterChoice.or)
            {
                foreach (var child in attr.ChildAttributes)
                {
                    var userName = SearchUserName(child);
                    if (userName != null) return userName;
                }
            }

            return null;
        }

        private LdapPacket InvalidCredentials(LdapPacket requestPacket)
        {
            var responsePacket = new LdapPacket(requestPacket.MessageId);
            responsePacket.ChildAttributes.Add(new LdapResultAttribute(LdapOperation.BindResponse, LdapResult.invalidCredentials));
            return responsePacket;
        }
        
        private LdapPacket ProfileNotFound(LdapPacket requestPacket)
        {
            var responsePacket = new LdapPacket(requestPacket.MessageId);
            responsePacket.ChildAttributes.Add(new LdapResultAttribute(LdapOperation.BindResponse, LdapResult.noSuchObject));
            return responsePacket;
        }

        private bool IsServiceAccount(string userName)
        {
            if (_clientConfig.ServiceAccounts.Any(acc => acc == userName.ToLower()))
            {
                return true;
            }

            if (_clientConfig.ServiceAccountsOrganizationUnit.Any(ou => userName.ToLower().Contains(ou)))
            {
                return true;
            }

            return false;
        }

        private bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf?.Any(g => g.ToLower() == group.ToLower().Trim()) ?? false;
        }

        private string ProcessUserNameTransformRules()
        {
            var userName = _userName;

            foreach (var rule in _clientConfig.UserNameTransformRules)
            {
                var regex = new Regex(rule.Match);
                var before = userName;
                if (rule.Count != null)
                {
                    userName = regex.Replace(userName, rule.Replace, rule.Count.Value);
                }
                else
                {
                    userName = regex.Replace(userName, rule.Replace);
                }

                if (before != userName)
                {
                    _logger.Debug($"Transformed username {before} => {userName}");
                }
            }

            return userName;
        }

        private async Task<string> EnforceLdapIdentityFormat(string baseDn, LdapIdentityFormat loginFormat)
        {
            if (loginFormat == LdapIdentityFormat.None)
            {
                throw new ArgumentException("Incorrect identity format was passed");
            }

            _logger.Debug($"{_userName} username will be transformed to {_clientConfig.TransformLdapIdentity} format"); ;
            var domains = await _ldapService.GetDomains(_serverStream, baseDn);
            var matchedProfile = await _ldapService.ResolveProfile(_serverStream, baseDn,_userName);
            if (matchedProfile == null)
            {
                _logger.Error($"{_userName} profile was not found, unable to translate the username");
                return _userName;
            }
            var context = new NameResolverContext()
            {
                Domains = domains,
                Profile = matchedProfile
            };

            return _nameResolverService.Resolve(context, _userName, loginFormat);
        }

    }

    public enum LdapProxyAuthenticationStatus
    {
        None,
        UserDnSearch,
        BindRequested,
        AuthenticationFailed
    }
}