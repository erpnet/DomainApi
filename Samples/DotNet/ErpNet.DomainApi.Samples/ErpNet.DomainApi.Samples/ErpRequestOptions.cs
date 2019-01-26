using System.Collections.Generic;

namespace ErpNet.DomainApi.Samples
{
    /// <summary>
    /// Request options provided with each API request.
    /// </summary>
    public class ErpRequestOptions
    {
        HashSet<string> options = new HashSet<string>();
        /// <summary>
        /// Indicates whether to skip the null-valued properties in the JSON result.
        /// </summary>
        public bool SkipNulls
        {
            get { return options.Contains("skipnulls"); }
            set { if (value) options.Add("skipnulls"); else options.Remove("skipnulls"); }
        }
        /// <summary>
        /// Gets or sets a value indicating whether to include entity Id in JSON result.
        /// </summary>
        public bool IncludeId
        {
            get { return options.Contains("includeid"); }
            set { if (value) options.Add("includeid"); else options.Remove("includeid"); }
        }

        public override string ToString()
        {
            return string.Join(",", options);
        }


    }
}
