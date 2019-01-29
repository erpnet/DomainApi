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
            var tr = await session.BeginTransactionAsync();

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
            var changes = await tr.Client.ExecuteFunctionAsEnumerableAsync("GetChanges", null);

            foreach (var item in changes)
            {
                foreach (var ch in item)
                {
                    var value = ch.Value;
                    if (value is string)
                    {
                        var jobj = JObject.Parse(ch.Value.ToString());
                        value = jobj.ToString(Newtonsoft.Json.Formatting.Indented);
                    }
                    Console.WriteLine("\r\n{0}:\r\n{1}", ch.Key, value);
                }
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

    }
}
