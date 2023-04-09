using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using WWB.OSS.Exceptions;
using WWB.OSS.Models;

namespace WWB.OSS
{
    public class OSSServiceFactory : IOSSServiceFactory
    {
        private readonly ILoggerFactory logger;
        private readonly IOptionsMonitor<OSSOptions> optionsMonitor;
        private static ConcurrentDictionary<string, IOSSService> _ossServiceDic = new ConcurrentDictionary<string, IOSSService>();
        private const string ASSEMBLY = "WWB.OSS.{0}";

        public OSSServiceFactory(IOptionsMonitor<OSSOptions> optionsMonitor, ILoggerFactory logger)
        {
            this.optionsMonitor = optionsMonitor ?? throw new ArgumentNullException();
            this.optionsMonitor = optionsMonitor ?? throw new ArgumentNullException();

            this.optionsMonitor = optionsMonitor;
            this.logger = logger;
        }

        public IOSSService Create()
        {
            return Create(DefaultOptionName.Name);
        }

        public IOSSService Create(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"Name can not null.");

            if (_ossServiceDic.ContainsKey(name))
            {
                return _ossServiceDic[name];
            }

            var options = optionsMonitor.Get(name);
            if (options == null)
                throw new ArgumentException($"Cannot get option by name '{name}'.");
            if (string.IsNullOrEmpty(options.Provider))
                throw new ArgumentNullException(nameof(options.Provider), "Provider can not null.");

            var type = GetType(options.Provider);
            var ossService = (IOSSService)Activator.CreateInstance(type, new object[] { options });
            if (ossService == null)
                throw new ArgumentNullException("ossService is null.");

            _ossServiceDic[name] = ossService;

            return ossService;
        }

        private Type GetType(string name)
        {
            var assembly = Assembly.Load(string.Format(ASSEMBLY, name));
            var type = assembly.GetTypes().Where(type => typeof(IOSSService).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract).FirstOrDefault();
            if (type == null)
                throw new OSSException("type is not found.");

            return type;
        }
    }
}