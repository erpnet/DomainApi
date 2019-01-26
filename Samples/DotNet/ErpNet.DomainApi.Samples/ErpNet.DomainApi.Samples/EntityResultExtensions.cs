using System.Collections.Generic;

namespace ErpNet.DomainApi.Samples
{
    public static class EntityResultExtensions
    {
        public static Simple.OData.Client.ODataEntryAnnotations Annotations(this IDictionary<string, object> entityObject)
        {
            return (Simple.OData.Client.ODataEntryAnnotations)entityObject["__annotations"];
        }
    }
}
