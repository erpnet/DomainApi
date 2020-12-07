using Simple.OData.Client;
using System;
using System.Collections.Generic;

namespace ErpNet.DomainApi.Samples
{
    public static class EntityResultExtensions
    {
        /// <summary>
        /// Gets a property value from a domain object represented by IDictionary.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static object GetNestedValue(this IDictionary<string, object> obj, string path)
        {
            if (obj == null)
                return null;
            var segments = path.Split('.', '/');
            object value = obj;
            List<string> sofar = new List<string>();
            foreach (var s in segments)
            {
                if (!(value is IDictionary<string, object> o))
                    throw new InvalidOperationException($"Path {string.Join(".", sofar)} evaluates to {value ?? "null"} which is not a valid domain object.");
                sofar.Add(s);
                if (!o.TryGetValue(s, out value))
                {
                    throw new KeyNotFoundException($"There is no property with the specified path {string.Join(".", sofar)}");
                }
            }
            return value;
        }

        public static ODataEntryAnnotations Annotations(this IDictionary<string, object> obj)
        {
            return (ODataEntryAnnotations)obj["__annotations"];
        }

        public static Guid Id(this IDictionary<string, object> obj)
        {
            return (Guid)obj["Id"];
        }

        public static IEnumerable<IDictionary<string, object>> AsEntityList(this object obj)
        {
            return (IEnumerable<IDictionary<string, object>>)obj;
        }
    }
}
