using System.Collections.Generic;
using System.Threading.Tasks;
using WWB.OSS.Aliyun.Model;

namespace WWB.OSS.Aliyun
{
    public interface IAliyunOSSService : IOSSService
    {
        /// <summary>
        /// 获取桶外部访问URL
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        Task<string> GetBucketEndpointAsync(string bucketName);

        /// <summary>
        /// 获取储存桶地域
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        Task<string> GetBucketLocationAsync(string bucketName);

        /// <summary>
        /// 管理桶跨域访问
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        Task<bool> SetBucketCorsRequestAsync(string bucketName, List<BucketCorsRule> rules);
    }
}