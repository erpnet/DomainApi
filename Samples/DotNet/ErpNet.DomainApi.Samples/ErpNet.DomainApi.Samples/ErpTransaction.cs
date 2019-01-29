using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErpNet.DomainApi.Samples
{
    /// <summary>
    /// Represents an API transaction.
    /// </summary>
    public class ErpTransaction
    {
        public ErpTransaction(ErpSession session, string transactionId)
        {
            TransactionId = transactionId;
            Client = ErpSession.CreateODataClient(session, transactionId);
        }

        /// <summary>
        /// Gets the odata client.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public ODataClient Client { get; }

        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        public string TransactionId { get; }

        /// <summary>
        /// Ends the transaction.
        /// </summary>
        /// <param name="commit">if set to <c>true</c> [commit].</param>
        /// <returns></returns>
        /// <exception cref="Exception">There is no current front end transaction.</exception>
        public Task<string> EndTransactionAsync(bool commit = true)
        {
            return Client.ExecuteActionAsScalarAsync<string>("EndTransaction", new Dictionary<string, object>() { ["commit"] = commit });
        }
    }
}
