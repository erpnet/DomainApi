using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErpNet.DomainApi.Samples
{
    static class Samples
    {


        public static async Task LoadProducts(ErpSession session)
        {
            var products = await session.Client.For("General_Products_Products")
                .Filter("Active eq true")
                .Top(10)
                .Expand("ProductGroup")
                .Select("PartNumber", "ProductGroup/Code")
                .FindEntriesAsync();

            foreach (var p in products)
                Console.WriteLine("{0}\t{1}", ((IDictionary<string, object>)p["ProductGroup"])["Code"], p["PartNumber"]);
        }

        public static async Task FilterByReference(ErpSession session)
        {
            var productGroup = await session.Client.For("General_Products_ProductGroups")
               .Filter("Active eq true")
               .Top(1)
               .Select("Id")
               .FindEntryAsync();

            // Use dynamic syntax
            var products = await session.Client.For("General_Products_Products")
               .Filter($"ProductGroup eq 'General_Products_ProductGroups({productGroup["Id"]})'")
               .Top(10)
               .Expand("ProductGroup")
               .Select("PartNumber", "ProductGroup/Code")
               .FindEntriesAsync();


            foreach (var p in products)
                Console.WriteLine("{0}\t{1}", ((IDictionary<string, object>)p["ProductGroup"])["Code"], p["PartNumber"]);
        }

        public static async Task UpdateProduct(ErpSession session)
        {
            var product = await session.Client.For("General_Products_Products")
                .Filter("PartNumber eq '1000044591'")
                .Top(1)
                .FindEntryAsync();

            var unit = await session.Client.For("General_MeasurementUnits")
                .Filter("Code eq 'бр'")
                .FindEntryAsync();

            await session.Client.For("General_Products_Products")
                .Key(product)
                .Set(new { MeasurementUnit = unit, ABCClass = "A", StandardLotSizeBase = new { Value = 3.45, Unit = "бр" } })
                .UpdateEntryAsync();
        }

        public static async Task FrontEndTransaction(ErpSession session)
        {
            var tr = await session.BeginTransactionAsync(ErpTransactionDataModel.FrontEnd, true);

            var order = await tr.Client.For("Crm_Sales_SalesOrders")
                .Filter("DocumentDate ge 2012-01-01T00:00:00Z and State eq 'FirmPlanned'")
                .Top(1)
                .FindEntryAsync();

            var customer = await tr.Client.For("Crm_Customers")
                .Top(1)
                .FindEntryAsync();

            await tr.Client.For("Crm_Sales_SalesOrders")
                .Key(order)
                .Set(new { Customer = customer })
                .UpdateEntryAsync();

            // Get the changes made by POST, PATCH and DELETE requests for the current front-end transaction.
            // Only the changes made after the last call of GenChanges are returned.
            var changes = await tr.Client.ExecuteFunctionAsSingleAsync("GetChanges", null);

            foreach (var operationEntry in changes)
            {
                // operationEntry.Key is one of: "insert", "update", "delete".
                // operationEntry.Value is a JSON hash table containing the changed objects divided by entity name and Id.
                var value = operationEntry.Value;
                if (value is string json)
                {
                    var jobj = JObject.Parse(json);
                    value = jobj.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                Console.WriteLine("\r\n{0}:\r\n{1}", operationEntry.Key, value);
            }

            // We don't commit here because this is only a test method.
            await tr.EndTransactionAsync(false);
        }

        public static async Task ChangeDocumentState(ErpSession session)
        {

            var order = await session.Client.For("Crm_Sales_SalesOrders")
                .Filter("DocumentDate ge 2012-01-01T00:00:00Z and State eq 'FirmPlanned'")
                .Select("Id")
                .Top(1)
                .FindEntryAsync();


            await session.Client.For("Crm_Sales_SalesOrders")
                .Key(order)
                .Action("ChangeState")
                .Set(new { newState = "FirmPlanned" })
                .ExecuteAsync();
        }

        public static async Task CreateDocumentAdjustment(ErpSession session)
        {
            // Begin a front-end transaction.
            var tr = await session.BeginTransactionAsync(ErpTransactionDataModel.Common, false);

            // Ordinary update for released documents is not allowed. 
            // The update is made through adjustment documents.
            // The API provides method to simplify the creation of adjustment documents: CreateAdjusmentDocuments.

            // Load a sales order that is on state Released.
            var order = await tr.Client.For("Crm_Sales_SalesOrders")
                .Key(Guid.Parse("e4528383-cb1a-4395-8eec-db9b87d78333"))
                .Select("Id,Lines")
                .Expand("Lines")
                .Top(1)
                .FindEntryAsync();

            var lines = (IEnumerable<IDictionary<string, object>>)order["Lines"];
            // Update some order lines. 
            // If we commit the front-end transaction an error will be thrown because editing released documents is not allowed.            
            // However we'll update the line and we'll not commit the transaction.
            foreach (var line in lines)
            {
                // Quantity is complex type consisted of Value and Unit.
                var quantity = (IDictionary<string, object>)line["Quantity"];
                decimal value = (decimal)quantity["Value"];
                string unit = (string)quantity["Unit"];
                value += 5;

                // Update the line
                await tr.Client.For("Crm_Sales_SalesOrderLines")
                    .Key(line["Id"])
                    .Set(new { Quantity = new { Value = value, Unit = unit } })
                    .UpdateEntryAsync();
            }

            // Call CreateAdjustmentDocuments on the transaction.
            // This method will create and apply adjustment documents for the modified released documents in the transaction.
            var result = await tr.Client.ExecuteActionAsScalarAsync<string>("CreateAdjustmentDocuments", null);

            // Rollback the front-end transaction.
            await tr.EndTransactionAsync(false);
        }
    }
}
