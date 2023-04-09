namespace WWB.OSS
{
    public class OSSOptions
    {
        /// <summary>
        /// 枚举，OOS提供商
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// 节点
        /// </summary>
        /// <remarks>
        /// 腾讯云中表示AppId
        /// </remarks>
        public string Endpoint { get; set; }

        /// <summary>
        /// AccessKey
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// SecretKey
        /// </summary>
        public string SecretKey { get; set; }

        private string _region = "us-east-1";

        /// <summary>
        /// 地域
        /// </summary>
        public string Region
        {
            get
            {
                return _region;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _region = "us-east-1";
                }
                else
                {
                    _region = value;
                }
            }
        }

        /// <summary>
        /// 是否启用HTTPS
        /// </summary>
        public bool IsEnableHttps { get; set; } = true;
    }
}