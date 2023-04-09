namespace WWB.OSS.Qiniu
{
    public class QiniuApi
    {
        public static string GetServiceApi(OSSOptions options)
        {
            return $"{(options.IsEnableHttps ? "https" : "http")}://uc.qbox.me";
        }

        public static string GetBaseApi(string host, OSSOptions ptions)
        {
            return $"{(ptions.IsEnableHttps ? "https" : "http")}://{host}";
        }
    }
}