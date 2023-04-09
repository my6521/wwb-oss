using Aliyun.OSS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WWB.OSS.Aliyun.Model;
using WWB.OSS.Exceptions;
using WWB.OSS.Models;

namespace WWB.OSS.Aliyun
{
    public class AliyunOSSService : BaseOSSService, IAliyunOSSService
    {
        private readonly OssClient _client;

        public AliyunOSSService(OSSOptions options) : base(options)
        {
            if (string.IsNullOrEmpty(options.SecretKey))
                throw new ArgumentNullException(nameof(options.SecretKey), "SecretKey can not null.");
            if (string.IsNullOrEmpty(options.AccessKey))
                throw new ArgumentNullException(nameof(options.AccessKey), "AccessKey can not null.");
            _client = new OssClient(options.Endpoint, options.AccessKey, options.SecretKey);
        }

        #region Bucket

        public Task<bool> BucketExistsAsync(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            var result = _client.DoesBucketExist(bucketName);

            return Task.FromResult(result);
        }

        public Task<bool> CreateBucketAsync(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            //检查桶是否存在
            var bucket = ListBucketsAsync().Result?.Where(p => p.Name == bucketName)?.FirstOrDefault();
            if (bucket != null)
            {
                var localtion = Options.Endpoint?.Split('.')[0];
                if (bucket.Location.Equals(localtion, StringComparison.OrdinalIgnoreCase))
                {
                    throw new OSSException($"Bucket '{bucketName}' already exists.");
                }
                else
                {
                    throw new OSSException($"There have a same name bucket '{bucketName}' in other localtion '{bucket.Location}'.");
                }
            }
            var request = new CreateBucketRequest(bucketName)
            {
                //设置存储空间访问权限ACL。
                ACL = CannedAccessControlList.Private,
                //设置数据容灾类型。
                DataRedundancyType = DataRedundancyType.LRS
            };
            // 创建存储空间。
            var result = _client.CreateBucket(request);
            return Task.FromResult(result != null);
        }

        public Task<bool> RemoveBucketAsync(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            _client.DeleteBucket(bucketName);

            return Task.FromResult(true);
        }

        public Task<List<Models.Bucket>> ListBucketsAsync()
        {
            var buckets = _client.ListBuckets();
            if (buckets == null)
            {
                return null;
            }
            if (buckets.Count() == 0)
            {
                return Task.FromResult(new List<Models.Bucket>());
            }
            var resultList = new List<Models.Bucket>();
            foreach (var item in buckets)
            {
                resultList.Add(new Models.Bucket()
                {
                    Location = item.Location,
                    Name = item.Name,
                    Owner = new Models.Owner()
                    {
                        Name = item.Owner.DisplayName,
                        Id = item.Owner.Id
                    },
                    CreationDate = item.CreationDate.ToString("yyyy-MM-dd HH:mm:ss"),
                });
            }
            return Task.FromResult(resultList);
        }

        public Task<bool> SetBucketAclAsync(string bucketName, AccessMode mode)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            var canned = mode switch
            {
                AccessMode.Default => CannedAccessControlList.Default,
                AccessMode.Private => CannedAccessControlList.Private,
                AccessMode.PublicRead => CannedAccessControlList.PublicRead,
                AccessMode.PublicReadWrite => CannedAccessControlList.PublicReadWrite,
                _ => CannedAccessControlList.Default,
            };
            var request = new SetBucketAclRequest(bucketName, canned);
            _client.SetBucketAcl(request);
            return Task.FromResult(true);
        }

        public Task<AccessMode> GetBucketAclAsync(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            var result = _client.GetBucketAcl(bucketName);
            var mode = result.ACL switch
            {
                CannedAccessControlList.Default => AccessMode.Default,
                CannedAccessControlList.Private => AccessMode.Private,
                CannedAccessControlList.PublicRead => AccessMode.PublicRead,
                CannedAccessControlList.PublicReadWrite => AccessMode.PublicReadWrite,
                _ => AccessMode.Default,
            };
            return Task.FromResult(mode);
        }

        #endregion Bucket

        #region Object

        public Task<bool> ObjectsExistsAsync(string bucketName, string objectName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            return Task.FromResult(_client.DoesObjectExist(bucketName, objectName));
        }

        public Task<List<Item>> ListObjectsAsync(string bucketName, string prefix = null)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            List<Item> result = new List<Item>();
            ObjectListing resultObj = null;
            string nextMarker = string.Empty;
            do
            {
                // 每页列举的文件个数通过maxKeys指定，超过指定数将进行分页显示。
                var listObjectsRequest = new ListObjectsRequest(bucketName)
                {
                    Marker = nextMarker,
                    MaxKeys = 100,
                    Prefix = prefix,
                };
                resultObj = _client.ListObjects(listObjectsRequest);
                if (resultObj == null)
                {
                    continue;
                }
                foreach (var item in resultObj.ObjectSummaries)
                {
                    result.Add(new Item()
                    {
                        Key = item.Key,
                        LastModified = item.LastModified.ToString(),
                        ETag = item.ETag,
                        Size = (ulong)item.Size,
                        BucketName = bucketName,
                        IsDir = !string.IsNullOrWhiteSpace(item.Key) && item.Key[^1] == '/',
                        LastModifiedDateTime = item.LastModified
                    });
                }
                nextMarker = resultObj.NextMarker;
            } while (resultObj.IsTruncated);
            return Task.FromResult(result);
        }

        public Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            var obj = _client.GetObject(bucketName, objectName);
            callback(obj.Content);
            return Task.CompletedTask;
        }

        public Task GetObjectAsync(string bucketName, string objectName, string fileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            string fullPath = Path.GetFullPath(fileName);
            string parentPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parentPath) && !Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }
            objectName = FormatObjectName(objectName);
            return GetObjectAsync(bucketName, objectName, (stream) =>
            {
                using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                    stream.Dispose();
                    fs.Close();
                }
            }, cancellationToken);
        }

        public Task<bool> PutObjectAsync(string bucketName, string objectName, Stream data, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            var result = _client.PutObject(bucketName, objectName, data);
            if (result != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> PutObjectAsync(string bucketName, string objectName, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            if (!File.Exists(filePath))
            {
                throw new Exception("Upload file is not exist.");
            }
            var result = _client.PutObject(bucketName, objectName, filePath);
            if (result != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<ItemMeta> GetObjectMetadataAsync(string bucketName, string objectName, string versionID = null, string matchEtag = null, DateTime? modifiedSince = null)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            GetObjectMetadataRequest request = new GetObjectMetadataRequest(bucketName, objectName)
            {
                VersionId = versionID
            };
            var oldMeta = _client.GetObjectMetadata(request);
            // 设置新的文件元信息。
            var newMeta = new ItemMeta()
            {
                ObjectName = objectName,
                ContentType = oldMeta.ContentType,
                Size = oldMeta.ContentLength,
                LastModified = oldMeta.LastModified,
                ETag = oldMeta.ETag,
                IsEnableHttps = Options.IsEnableHttps,
                MetaData = new Dictionary<string, string>(),
            };
            if (oldMeta.UserMetadata != null && oldMeta.UserMetadata.Count > 0)
            {
                foreach (var item in oldMeta.UserMetadata)
                {
                    newMeta.MetaData.Add(item.Key, item.Value);
                }
            }
            return Task.FromResult(newMeta);
        }

        public Task<bool> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            if (string.IsNullOrEmpty(destBucketName))
            {
                destBucketName = bucketName;
            }
            destObjectName = FormatObjectName(destObjectName);
            var partSize = 50 * 1024 * 1024;
            // 创建OssClient实例。
            // 初始化拷贝任务。可以通过InitiateMultipartUploadRequest指定目标文件元信息。
            var request = new InitiateMultipartUploadRequest(destBucketName, destObjectName);
            var result = _client.InitiateMultipartUpload(request);
            // 计算分片数。
            var metadata = _client.GetObjectMetadata(bucketName, objectName);
            var fileSize = metadata.ContentLength;
            var partCount = (int)fileSize / partSize;
            if (fileSize % partSize != 0)
            {
                partCount++;
            }
            // 开始分片拷贝。
            var partETags = new List<PartETag>();
            for (var i = 0; i < partCount; i++)
            {
                var skipBytes = (long)partSize * i;
                var size = (partSize < fileSize - skipBytes) ? partSize : (fileSize - skipBytes);
                // 创建UploadPartCopyRequest。可以通过UploadPartCopyRequest指定限定条件。
                var uploadPartCopyRequest = new UploadPartCopyRequest(destBucketName, destObjectName, bucketName, objectName, result.UploadId)
                {
                    PartSize = size,
                    PartNumber = i + 1,
                    // BeginIndex用来定位此次上传分片开始所对应的位置。
                    BeginIndex = skipBytes
                };
                // 调用uploadPartCopy方法来拷贝每一个分片。
                var uploadPartCopyResult = _client.UploadPartCopy(uploadPartCopyRequest);
                partETags.Add(uploadPartCopyResult.PartETag);
            }
            // 完成分片拷贝。
            var completeMultipartUploadRequest =
            new CompleteMultipartUploadRequest(destBucketName, destObjectName, result.UploadId);
            // partETags为分片上传中保存的partETag的列表，OSS收到用户提交的此列表后，会逐一验证每个数据分片的有效性。全部验证通过后，OSS会将这些分片合成一个完整的文件。
            foreach (var partETag in partETags)
            {
                completeMultipartUploadRequest.PartETags.Add(partETag);
            }
            _client.CompleteMultipartUpload(completeMultipartUploadRequest);
            return Task.FromResult(true);
        }

        public Task<bool> RemoveObjectAsync(string bucketName, string objectName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            var result = _client.DeleteObject(bucketName, objectName);
            if (result != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> RemoveObjectAsync(string bucketName, List<string> objectNames)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            if (objectNames == null || objectNames.Count == 0)
            {
                throw new ArgumentNullException(nameof(objectNames));
            }
            var delObjects = new List<string>();
            foreach (var item in objectNames)
            {
                delObjects.Add(FormatObjectName(item));
            }
            var quietMode = false;
            // DeleteObjectsRequest的第三个参数指定返回模式。
            var request = new DeleteObjectsRequest(bucketName, delObjects, quietMode);
            // 删除多个文件。
            var result = _client.DeleteObjects(request);
            if ((!quietMode) && (result.Keys != null))
            {
                if (result.Keys.Count() == delObjects.Count)
                {
                    return Task.FromResult(true);
                }
                else
                {
                    throw new Exception("Some file delete failed.");
                }
            }
            else
            {
                if (result != null)
                {
                    return Task.FromResult(true);
                }
                else
                {
                    return Task.FromResult(true);
                }
            }
        }

        public async Task<bool> SetObjectAclAsync(string bucketName, string objectName, AccessMode mode)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            if (!await this.ObjectsExistsAsync(bucketName, objectName))
            {
                throw new Exception($"Object '{objectName}' not in bucket '{bucketName}'.");
            }
            var canned = mode switch
            {
                AccessMode.Default => CannedAccessControlList.Default,
                AccessMode.Private => CannedAccessControlList.Private,
                AccessMode.PublicRead => CannedAccessControlList.PublicRead,
                AccessMode.PublicReadWrite => CannedAccessControlList.PublicReadWrite,
                _ => CannedAccessControlList.Default,
            };
            _client.SetObjectAcl(bucketName, objectName, canned);
            return await Task.FromResult(true);
        }

        public async Task<AccessMode> GetObjectAclAsync(string bucketName, string objectName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            if (!await this.ObjectsExistsAsync(bucketName, objectName))
            {
                throw new Exception($"Object '{objectName}' not in bucket '{bucketName}'.");
            }
            var result = _client.GetObjectAcl(bucketName, objectName);
            var mode = result.ACL switch
            {
                CannedAccessControlList.Default => AccessMode.Default,
                CannedAccessControlList.Private => AccessMode.Private,
                CannedAccessControlList.PublicRead => AccessMode.PublicRead,
                CannedAccessControlList.PublicReadWrite => AccessMode.PublicReadWrite,
                _ => AccessMode.Default,
            };
            if (mode == AccessMode.Default)
            {
                return await GetBucketAclAsync(bucketName);
            }
            return await Task.FromResult(mode);
        }

        public async Task<AccessMode> RemoveObjectAclAsync(string bucketName, string objectName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            objectName = FormatObjectName(objectName);
            if (!await SetObjectAclAsync(bucketName, objectName, AccessMode.Default))
            {
                throw new Exception("Save new policy info failed when remove object acl.");
            }
            return await GetObjectAclAsync(bucketName, objectName);
        }

        public async Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            objectName = FormatObjectName(objectName);
            //生成URL
            var accessMode = await this.GetObjectAclAsync(bucketName, objectName);

            if (accessMode == AccessMode.PublicRead || accessMode == AccessMode.PublicReadWrite)
            {
                string bucketUrl = await this.GetBucketEndpointAsync(bucketName);
                string uri = $"{bucketUrl}{(objectName.StartsWith("/") ? "" : "/")}{objectName}";
                return uri;
            }
            else
            {
                var request = new GeneratePresignedUriRequest(bucketName, objectName, SignHttpMethod.Get)
                {
                    Expiration = DateTime.Now.AddSeconds(expiresInt)
                };
                var uri = _client.GeneratePresignedUri(request);
                if (uri == null)
                {
                    throw new Exception("Generate get presigned uri failed");
                }

                return uri.ToString();
            }
        }

        #endregion Object

        #region IAliyunOSSService

        public Task<string> GetBucketEndpointAsync(string bucketName)
        {
            var result = _client.GetBucketInfo(bucketName);
            if (result != null
                && result.Bucket != null
                && !string.IsNullOrEmpty(result.Bucket.Name)
                && !string.IsNullOrEmpty(result.Bucket.ExtranetEndpoint))
            {
                string host = $"{(Options.IsEnableHttps ? "https://" : "http://")}{result.Bucket.Name}.{result.Bucket.ExtranetEndpoint}";
                return Task.FromResult(host);
            }
            return Task.FromResult(string.Empty);
        }

        public Task<string> GetBucketLocationAsync(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            var result = _client.GetBucketLocation(bucketName);
            if (result == null)
            {
                return null;
            }
            return Task.FromResult(result.Location);
        }

        public Task<bool> SetBucketCorsRequestAsync(string bucketName, List<BucketCorsRule> rules)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }
            if (rules == null || rules.Count == 0)
            {
                throw new ArgumentNullException(nameof(rules));
            }
            var request = new SetBucketCorsRequest(bucketName);
            foreach (var item in rules)
            {
                var rule = new CORSRule();
                // 指定允许跨域请求的来源。
                rule.AddAllowedOrigin(item.Origin);
                // 指定允许的跨域请求方法(GET/PUT/DELETE/POST/HEAD)。
                rule.AddAllowedMethod(item.Method.ToString());
                // AllowedHeaders和ExposeHeaders不支持通配符。
                rule.AddAllowedHeader(item.AllowedHeader);
                // 指定允许用户从应用程序中访问的响应头。
                rule.AddExposeHeader(item.ExposeHeader);

                request.AddCORSRule(rule);
            }
            // 设置跨域资源共享规则。
            _client.SetBucketCors(request);
            return Task.FromResult(true);
        }

        #endregion IAliyunOSSService
    }
}