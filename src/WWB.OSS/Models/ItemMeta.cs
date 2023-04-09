using System;
using System.Collections.Generic;

namespace WWB.OSS.Models
{
    public class ItemMeta
    {
        public string ObjectName { get; set; }

        public long Size { get; set; }

        public DateTime LastModified { get; set; }

        public string ETag { get; set; }

        public string ContentType { get; set; }

        public bool IsEnableHttps { get; set; }

        public Dictionary<string, string> MetaData { get; set; }
    }
}