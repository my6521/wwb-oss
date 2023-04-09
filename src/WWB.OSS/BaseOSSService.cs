using System;

namespace WWB.OSS
{
    public abstract class BaseOSSService
    {
        public OSSOptions Options { get; private set; }

        public BaseOSSService(OSSOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected virtual string FormatObjectName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName) || objectName == "/")
            {
                throw new ArgumentNullException(nameof(objectName));
            }
            if (objectName.StartsWith('/'))
            {
                return objectName.TrimStart('/');
            }
            return objectName;
        }
    }
}