using System;

namespace WWB.OSS.Qiniu
{
    public class QiniuOSSService : BaseOSSService, IQiniuOSSService
    {
        public QiniuOSSService(OSSOptions options) : base(options)
        {
            if (string.IsNullOrEmpty(options.SecretKey))
                throw new ArgumentNullException(nameof(options.SecretKey), "SecretKey can not null.");
            if (string.IsNullOrEmpty(options.AccessKey))
                throw new ArgumentNullException(nameof(options.AccessKey), "AccessKey can not null.");
            if (string.IsNullOrEmpty(options.Region))
                throw new ArgumentNullException(nameof(options.Region), "Region can not null.");
        }
    }
}