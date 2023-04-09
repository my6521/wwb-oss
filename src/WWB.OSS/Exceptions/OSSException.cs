using System;

namespace WWB.OSS.Exceptions
{
    public class OSSException : Exception
    {
        public OSSException()
        {
        }

        public OSSException(string message) : base(message)
        {
        }

        public OSSException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}