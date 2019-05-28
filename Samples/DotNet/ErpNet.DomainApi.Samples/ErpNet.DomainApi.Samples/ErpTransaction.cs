using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErpNet.DomainApi.Samples
{
    /// <summary>
    /// Represents an API front-end transaction.
    /// </summary>
    public class ErpFrontEndTransaction
    {
        public ErpFrontEndTransaction(ErpSession session, string transactionId)
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
        /// <returns>The transaction id</returns>        
        public Task<string> EndFrontEndTransactionAsync(bool commit = true)
        {
            return Client.ExecuteActionAsScalarAsync<string>(
                "EndFrontEndTransaction",
                new Dictionary<string, object>() { ["commit"] = commit });
        }

        /// <summary>
        /// Creates adjustments for modified released documents. 
        /// The adjusted documents are created in separate transaction and their state is changed to 'Adjustment'. 
        /// The method does not commit or rollback the current front-end transaction.
        /// </summary>
        /// <exception cref="AggregateException">Error while applying adjusting documents. Note that adjusting documents are created but they may be at state 'New'.</exception>
        public Task CreateAdjustmentDocuments()
        {
            return Client.ExecuteActionAsScalarAsync<string>("CreateAdjustmentDocuments", null);
        }
    }
}
