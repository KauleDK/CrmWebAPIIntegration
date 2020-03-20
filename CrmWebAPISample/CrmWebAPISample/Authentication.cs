using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Services.AppAuthentication;

namespace CrmWebAPISample
{
    public class Authentication
    {
        Configuration config;

        public Authentication(Configuration config)
        {
            this.config = config;
            this.ClientHandler = new OAuthMessageHandler(this, new HttpClientHandler());
            this.Context = new AuthenticationContext(config.AuthorityUrl);
        }

        public HttpMessageHandler ClientHandler { get; } = null;

        public AuthenticationContext Context { get; set; }

        public async Task<string> AcquireTokenWithClientSecret()
        {
            var cred = new ClientCredential(config.ClientId, config.ClientSecret);
            AuthenticationResult authenticationResult = await Context.AcquireTokenAsync(config.ServiceUrl, cred);

            return authenticationResult.AccessToken;
        }

        public async Task<string> AcquireTokenWithManagedIdentity()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            // access token through manage identity
            return await azureServiceTokenProvider.GetAccessTokenAsync(config.ServiceUrl);
        }

        public async Task<string> AcquireTokenAsUser()
        {
            var clientId = config.ClientId;
            var clientSecret = config.ClientSecret;
            var userName = config.UserName;
            var password = config.Password;
            var tokenEndpoint = $"{config.AuthorityUrl}/oauth2/token";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            string postBody = $"resource={config.ServiceUrl}&client_id={clientId}&client_secret={clientSecret}&grant_type=password&username={userName}&password={password}&scope=openid";

            using (var response = await httpClient.PostAsync(tokenEndpoint, new StringContent(postBody, Encoding.UTF8, "application/x-www-form-urlencoded")))
            {
                try
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonresult = JObject.Parse(await response.Content.ReadAsStringAsync());
                        var accessToken = (string)jsonresult["access_token"];
                        return accessToken;
                    }
                    throw new InvalidOperationException($"Unable to obtain access token, response was: {await response.Content.ReadAsStringAsync()}");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Exeption obtaning access token, response was: {await response.Content.ReadAsStringAsync()}, expection was: {ex}");
                }
            }
        }      

        class OAuthMessageHandler : DelegatingHandler
        {
            Authentication auth = null;

            public OAuthMessageHandler(Authentication auth, HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
                this.auth = auth;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                // It is a best practice to refresh the access token before every message request is sent. Doing so
                // avoids having to check the expiration date/time of the token. This operation is quick.
                //string token = auth.AcquireTokenAsApp().Result;
                string token = auth.AcquireTokenAsUser().Result;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
