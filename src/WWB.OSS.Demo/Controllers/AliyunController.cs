using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WWB.OSS.Demo.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AliyunController : ControllerBase
    {
        private readonly ILogger<AliyunController> _logger;
        private readonly IOSSServiceFactory _oSSServiceFactory;

        public AliyunController(ILogger<AliyunController> logger, IOSSServiceFactory oSSServiceFactory)
        {
            _logger = logger;
            _oSSServiceFactory = oSSServiceFactory;
        }

        [HttpGet]
        public async Task<IActionResult> BucketExists(string bucketName)
        {
            var ossService = _oSSServiceFactory.Create();
            var exist = await ossService.BucketExistsAsync(bucketName);

            return Ok(exist);
        }
    }
}