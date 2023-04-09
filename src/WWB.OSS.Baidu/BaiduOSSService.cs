using System;

namespace WWB.OSS.Baidu
{
    public class BaiduOSSService : BaseOSSService, IBaiduOSSService
    {
        public BaiduOSSService(OSSOptions options) : base(options)
        {
            if (string.IsNullOrEmpty(options.SecretKey))
                throw new ArgumentNullException(nameof(options.SecretKey), "SecretKey can not null.");
            if (string.IsNullOrEmpty(options.AccessKey))
                throw new ArgumentNullException(nameof(options.AccessKey), "AccessKey can not null.");
        }
    }
}