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
            ServiceRoot = serviceRoot;
            Client = CreateODataClient(this);
            httpClient.DefaultRequestHeaders.ConnectionClose = false;
        }

        internal static ODataClient CreateODataClient(ErpSession session, string transactionId = null)
        {
            var settings = new ODataClientSettings()
            {
                BaseUri = session.ServiceRoot,
                IncludeAnnotationsInResults = true,                
                IgnoreUnmappedProperties = true,
                BeforeRequest = (req) =>
                {
                    // Debug POST/PATCH requests
                    if (req.Content != null)
                        req.Content.ReadAsStringAsync().ContinueWith(t => Console.WriteLine(t.Result));
                    var options = session.RequestOptions.ToString();
                    if (!string.IsNullOrEmpty(options))
                    {
                        if (!string.IsNullOrEmpty(req.RequestUri.Query))
                            options = "&options=" + options;
                        else
                            options = "?options=" + options;
                        req.RequestUri = new Uri(req.RequestUri + options);
                    }
                    if (session.authorizationHeader != null)
                        req.Headers.Add("Authorization", session.authorizationHeader);
                    if (transactionId != null)
                        req.Headers.Add("TransactionId", transactionId);
                },
                AfterResponse = async (resp) =>
                {

                    if (session.Metadata == null && resp.RequestMessage.RequestUri.ToString().Contains("$metadata"))
                    {
                        session.Metadata = await resp.Content.ReadAsStringAsync();
                    }

                },
                OnTrace = (str, o) =>
                {
                    Console.WriteLine(str, o);
                },
                PayloadFormat = ODataPayloadFormat.Json
                //IncludeAnnotationsInResults = true
            };
            if (session.Metadata != null)
                settings.MetadataDocument = session.Metadata;
            return new ODataClient(settings);
        }

        /// <summary>
        /// Gets the ODATA service root.
        /// </summary>
        /// <value>
        /// The service root.
        /// </value>
        public Uri ServiceRoot { get; }
        /// <summary>
        /// Gets the <see cref="Simple.OData.Client"/> instance used to execute queries to the API.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public ODataClient Client { get; }

        /// <summary>
        /// Gets the $metadata xml.
        /// </summary>
        /// <value>
        /// The last response.
        /// </value>
        public string Metadata { get; private set; }

        /// <summary>
        /// Gets the request options used for this session.
        /// </summary>
        /// <value>
        /// The request options.
        /// </value>
        public ErpRequestOptions RequestOptions { get; } = new ErpRequestOptions();


        /// <summary>
        /// Begins an API transaction.
        /// </summary>
        /// <param name="trackChanges">if set to <c>true</c> track changes is enabled. That means that functions GetChanges and WaitForChanges can be used.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Only one front end transaction per session is allowed.</exception>
        public async Task<ErpTransaction> BeginTransactionAsync(ErpTransactionDataModel model, bool trackChanges)
        {
            var transactionId = await Client.ExecuteActionAsScalarAsync<string>(
                "BeginTransaction",
                new Dictionary<string, object>()
                {
                    ["model"] = model.ToString().ToLower(),
                    ["trackChanges"] = trackChanges
                });
            return new ErpTransaction(this, transactionId);
        }



        /// <summary>
        /// Closes the session.
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            if (authorizationHeader != null)
            {
                StringContent content = new StringContent("");
                content.Headers.ContentType.MediaType = "application/json";


                var uri = ServiceRoot.ToString().Replace("/odata", "/Logout").TrimEnd('/');
                var result = await httpClient.PostAsync(uri, content);

                //var json = await result.Content.ReadAsStringAsync();
                authorizationHeader = null;
            }
        }

        /// <summary>
        /// Logins the specified user.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Invalid user name or password.</exception>
        public async Task<string> LoginAsync(ErpCredentials credentials)
        {
            StringContent content = new StringContent(
                string.Format("{{\"app\":\"{0}\",\"user\":\"{1}\",\"pass\":\"{2}\",\"ln\":\"{3}\"}}",
                credentials.ApplicationName,
                credentials.UserName,
                credentials.Password,
                credentials.Language));
            content.Headers.ContentType.MediaType = "application/json";
            var uri = ServiceRoot.ToString().Replace("/odata", "/Login").TrimEnd('/');

            HttpRequestMessage msg = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Method = HttpMethod.Post,
                Content = content
            };
            msg.Headers.ExpectContinue = false;

            var result = await httpClient.SendAsync(msg, HttpCompletionOption.ResponseContentRead);
            var json = await result.Content.ReadAsStringAsync();
            try
            {
                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception(json);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid user name or password.", ex);
            }
               
            
            authorizationHeader = json.Split(':')[1].Trim('"', '}');
            httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeader);
            return json;
        }



        /// <summary>
        /// Calls <see cref="CloseAsync"/> without await.
        /// </summary>
        public void Dispose()
        {
            CloseAsync().ContinueWith(
                t =>
                {
                    if (t.Exception != null)
                        Console.WriteLine(t.Exception);
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
