using System;

namespace WWB.OSS.Models
{
    public class Item
    {
        public string Key { get; set; }

        public string LastModified { get; set; }

        public string ETag { get; set; }

        public ulong Size { get; set; }

        public bool IsDir { get; set; }

        public string BucketName { get; set; }

        public string VersionId { get; set; }

        public DateTime? LastModifiedDateTime { get; set; }
    }
}