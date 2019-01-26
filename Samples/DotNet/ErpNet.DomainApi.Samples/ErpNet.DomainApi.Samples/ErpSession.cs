using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ErpNet.DomainApi.Samples
{
    /// <summary>
    /// Represents a session to the damain API.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class ErpSession : IDisposable
    {

        string authorizationHeader = null;
        string lastResponse = null;
        Uri serviceRoot;
        HttpClient httpClient = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true,

            UseProxy = false,
            Proxy = null,
            AllowAutoRedirect = true
        });


        /// <summary>
        /// Initializes a new instance of the <see cref="ErpSession"/> class.
        /// </summary>
        /// <param name="serviceRoot">The service root.</param>
        public ErpSession(Uri serviceRoot)
        {
            this.serviceRoot = serviceRoot;

            Client = new ODataClient(new ODataClientSettings()
            {
                BaseUri = serviceRoot,
                BeforeRequest = (req) =>
                {
                    //if (req.Content != null)
                    //    req.Content.ReadAsStringAsync().ContinueWith(t => Console.WriteLine(t.Result));
                    var options = RequestOptions.ToString();
                    if (!string.IsNullOrEmpty(options))
                    {
                        if (!string.IsNullOrEmpty(req.RequestUri.Query))
                            options = "&options=" + options;
                        else
                            options = "?options=" + options;
                        req.RequestUri = new Uri(req.RequestUri + options);
                    }
                    if (authorizationHeader != null)
                        req.Headers.Add("Authorization", authorizationHeader);
                    if (TransactionId != null)
                        req.Headers.Add("TransactionId", TransactionId);
                },
                AfterResponse = async (resp) =>
                {
                    lastResponse = await resp.Content.ReadAsStringAsync();
                },
                OnTrace = (str, o) =>
                {
                    //Console.WriteLine(str, o);
                },
                PayloadFormat = ODataPayloadFormat.Json
                //IncludeAnnotationsInResults = true
            });
            httpClient.DefaultRequestHeaders.ConnectionClose = false;
        }

        /// <summary>
        /// Gets the <see cref="Simple.OData.Client"/> instance used to execute queries to the API.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public ODataClient Client { get; }

        /// <summary>
        /// Gets the last response as a string.
        /// </summary>
        /// <value>
        /// The last response.
        /// </value>
        public string LastResponse => lastResponse;

        /// <summary>
        /// Gets the request options used for this session.
        /// </summary>
        /// <value>
        /// The request options.
        /// </value>
        public ErpRequestOptions RequestOptions { get; } = new ErpRequestOptions();
        public string TransactionId { get; private set; }

        public async Task<string> BeginTransactionAsync()
        {
            if (TransactionId != null)
                throw new Exception("Only one front end transaction per session is allowed.");

            TransactionId = await Client.ExecuteActionAsScalarAsync<string>("BeginTransaction", new Dictionary<string, object>());
            return TransactionId;
        }

        public Task<string> EndTransactionAsync(bool commit = true)
        {
            if (TransactionId == null)
                throw new Exception("There is no current front end transaction.");

            return Client.ExecuteActionAsScalarAsync<string>("EndTransaction", new Dictionary<string, object>() { ["commit"] = commit });
        }

        public async void CloseAsync()
        {
            if (authorizationHeader != null)
            {
                StringContent content = new StringContent("");
                content.Headers.ContentType.MediaType = "application/json";


                var uri = serviceRoot.ToString().Replace("/odata", "/Logout").TrimEnd('/');
                var result = await httpClient.PostAsync(uri, content);

                var json = await result.Content.ReadAsStringAsync();
                authorizationHeader = null;
            }
        }

        public async Task<string> LoginAsync(ErpCredentials credentials)
        {
            StringContent content = new StringContent(
                string.Format("{{app:'{0}',user:'{1}',pass:'{2}',ln:'{3}'}}",
                credentials.ApplicationName,
                credentials.UserName,
                credentials.Password,
                credentials.Language));
            content.Headers.ContentType.MediaType = "application/json";
            var uri = serviceRoot.ToString().Replace("/odata", "/Login").TrimEnd('/');

            HttpRequestMessage msg = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Method = HttpMethod.Post,
                Content = content
            };
            msg.Headers.ExpectContinue = false;

            var result = await httpClient.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);

            if (!result.IsSuccessStatusCode)
                throw new Exception("Invalid user name or password.");
            var json = await result.Content.ReadAsStringAsync();
            authorizationHeader = json.Split(':')[1].Trim('"', '}');
            httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeader);
            return json;
        }



        public void Dispose()
        {
            CloseAsync();
        }
    }
}
