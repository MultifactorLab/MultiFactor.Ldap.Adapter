﻿//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md


using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services.Caching;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Services
{
    public class ConnectedClientInfo
    {
        public string Username { get; }
        public ClientConfiguration ClientConfiguration { get; }

        public ConnectedClientInfo(string username, ClientConfiguration clientConfiguration)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException($"'{nameof(username)}' cannot be null or empty.", nameof(username));
            }

            Username = username;
            ClientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
        }
    }

    /// <summary>
    /// Service to interact with multifactor web api
    /// </summary>
    public class MultiFactorApiClient
    {
        private readonly ServiceConfiguration _configuration;
        private readonly AuthenticatedClientCache _clientCache;
        private readonly IHttpClientFactory _httpClientFactory;
        readonly JsonSerializerOptions _serialazerOptions;
        private readonly ILogger _logger;

        public MultiFactorApiClient(ServiceConfiguration configuration, AuthenticatedClientCache clientCache, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _clientCache = clientCache ?? throw new ArgumentNullException(nameof(clientCache));
            _httpClientFactory = httpClientFactory;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serialazerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<bool> Authenticate(ConnectedClientInfo connectedClient, PersonalData personalData)
        {
            //try to get authenticated client to bypass second factor if configured
            if (_clientCache.TryHitCache(connectedClient.Username, connectedClient.ClientConfiguration))
            {
                _logger.Information("Bypass second factor for user '{user:l}'", connectedClient.Username);
                return true;
            }

            var url = _configuration.ApiUrl + "/access/requests/la";
            var payload = new
            {
                Identity = connectedClient.Username,
                Email = personalData.Email
            };

            var response = await SendRequest(connectedClient.ClientConfiguration, url, payload);

            if (response == null)
            {
                return false;
            }

            if (response.Granted && !response.Bypassed)
            {
                _logger.Information("Second factor for user '{user:l}' verified successfully. Authenticator '{authenticator:l}', account '{account:l}'",
                    connectedClient.Username, response?.Authenticator, response?.Account);
                _clientCache.SetCache(connectedClient.Username, connectedClient.ClientConfiguration);
            }

            if (response.Denied)
            {
                var reason = response?.ReplyMessage;
                var phone = response?.Phone;
                _logger.Warning("Second factor verification for user '{user:l}' failed with reason='{reason:l}'. User phone {phone:l}", 
                    connectedClient.Username, reason, phone);
            }

            return response.Granted;
        }

        private async Task<MultiFactorAccessRequest> SendRequest(ClientConfiguration clientConfig, string url, object payload)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.DefaultConnectionLimit = 100;

                var json = JsonSerializer.Serialize(payload, _serialazerOptions);

                _logger.Debug($"Sending request to API: {json}");

                //basic authorization
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(clientConfig.MultifactorApiKey + ":" + clientConfig.MultifactorApiSecret));
                var httpClient = _httpClientFactory.CreateClient(nameof(MultiFactorApiClient));

                StringContent jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = jsonContent
                };
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
                var res = await httpClient.SendAsync(message);
                
                if ((int)res.StatusCode == 429)
                {
                    _logger.Warning("Got unsuccessful response from API: {@response}", res.ReasonPhrase);
                    return new MultiFactorAccessRequest() { Status = "Denied", ReplyMessage = "Too many requests"};
                }
                
                var jsonResponse = await res.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<MultiFactorApiResponse<MultiFactorAccessRequest>>(jsonResponse, _serialazerOptions);

                _logger.Debug("Received response from API: {@response}", response);

                if (!response.Success)
                {
                    _logger.Warning("Got unsuccessful response from API: {@response}", response);
                }

                return response.Model;
            }
            catch (TaskCanceledException tce)
            {
                _logger.Error(tce, $"Multifactor API host unreachable {url}: timeout!");

                if (clientConfig.BypassSecondFactorWhenApiUnreachable)
                {
                    _logger.Warning("Bypass second factor");
                    return MultiFactorAccessRequest.Bypass;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Multifactor API host unreachable {url}: {ex.Message}");

                if (clientConfig.BypassSecondFactorWhenApiUnreachable)
                {
                    _logger.Warning("Bypass second factor");
                    return MultiFactorAccessRequest.Bypass;
                }

                return null;
            }
        }
    }

    public class MultiFactorApiResponse<TModel>
    {
        public bool Success { get; set; }

        public TModel Model { get; set; }
    }

    public class MultiFactorAccessRequest
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public string ReplyMessage { get; set; }
        public bool Bypassed { get; set; }
        public string Authenticator { get; set; }
        public string Account { get; set; }

        public bool Granted => Status == "Granted";
        public bool Denied => Status == "Denied";

        public static MultiFactorAccessRequest Bypass
        {
            get
            {
                return new MultiFactorAccessRequest { Status = "Granted", Bypassed = true };
            }
        }
    }
}
