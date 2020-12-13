using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// API version information
    /// </summary>
    public class Version
    {
        /// <summary>
        /// Instance identifier. Every application runtime has its own guid. If 3 pods are launched in kubernetes, it is possible to identify instance by this identifier
        /// </summary>
        public string InstanceIdentifier { get; set; }
        /// <summary>
        /// Last time when instance has been reset
        /// </summary>
        public string InstanceStartedAt { get; set; }
        /// <summary>
        /// Application name
        /// </summary>
        public string ApplicationName { get; set; } = "CovidMassTestingAPI";
        /// <summary>
        /// Docker image version
        /// </summary>
        public string DockerImageVersion { get; set; }
        /// <summary>
        /// Build number from devops or github actions
        /// </summary>
        public string BuildNumber { get; set; }
        /// <summary>
        /// Application dll version
        /// </summary>
        public string DLLVersion { get; set; }
        /// <summary>
        /// Dll build time
        /// </summary>
        public string BuildTime { get; set; }
        /// <summary>
        /// Culture info
        /// </summary>
        public string Culture { get; set; } = CultureInfo.CurrentCulture.Name;
        /// <summary>
        /// Shows info weather email service is configured
        /// </summary>
        public bool EmailConfigured { get; set; }
        /// <summary>
        /// Shows info weather persistant redis service is configured
        /// </summary>
        public bool RedisConfigured { get; set; }
        /// <summary>
        /// Shows info weather SMS service is configured
        /// </summary>
        public bool SMSConfigured { get; set; }
        /// <summary>
        /// Storage test
        /// </summary>
        public string StorageTest { get; set; }

        /// <summary>
        /// Get version
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="start"></param>
        /// <param name="dllVersion"></param>
        /// <param name="configuration"></param>
        /// <param name="visitorRepository"></param>
        /// <returns></returns>
        public async static Task<Version> GetVersion(string instanceId, DateTimeOffset start, string dllVersion, IConfiguration configuration, IVisitorRepository visitorRepository)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                throw new ArgumentException($"'{nameof(instanceId)}' cannot be null or empty", nameof(instanceId));
            }

            if (string.IsNullOrEmpty(dllVersion))
            {
                throw new ArgumentException($"'{nameof(dllVersion)}' cannot be null or empty", nameof(dllVersion));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (visitorRepository is null)
            {
                throw new ArgumentNullException(nameof(visitorRepository));
            }

            var ret = new Version();
            var versionFile = "version.txt";
            if (System.IO.File.Exists(versionFile))
            {
                var version = System.IO.File.ReadAllText(versionFile).Trim();
                var versionData = version.Split('|');
                if (versionData.Length == 3)
                {
                    var pos = versionData[0].LastIndexOf('-');
                    if (pos > 0)
                    {
                        ret.ApplicationName = versionData[0].Substring(0, pos - 1).Trim();
                        ret.BuildNumber = versionData[0][(pos + 1)..].Trim();
                    }
                    ret.DLLVersion = versionData[1].Trim();
                    ret.BuildTime = versionData[2].Trim();
                }
            }
            else
            {
                ret.DLLVersion = dllVersion;
            }
            var versionFileDocker = "docker-version.txt";
            if (System.IO.File.Exists(versionFileDocker))
            {
                ret.DockerImageVersion = System.IO.File.ReadAllText(versionFileDocker).Trim();
            }
            ret.InstanceStartedAt = start.ToString("o");
            ret.InstanceIdentifier = instanceId;

            var redisConfiguration = new RedisConfiguration();
            try
            {
                configuration.GetSection("Redis")?.Bind(redisConfiguration);
            }
            catch { }

            if (!string.IsNullOrEmpty(redisConfiguration.Hosts?.FirstOrDefault()?.Host))
            {
                ret.RedisConfigured = true;
            }
            try
            {
                ret.StorageTest = (await visitorRepository.TestStorage()).ToString();
            }
            catch (Exception exc)
            {
                ret.StorageTest = exc.Message;
            }
            var goSMSConfiguration = new Model.Settings.GoSMSConfiguration();
            try
            {
                configuration.GetSection("GoSMS")?.Bind(goSMSConfiguration);
            }
            catch { }
            if (!string.IsNullOrEmpty(goSMSConfiguration.ClientId))
            {
                ret.SMSConfigured = true;
            }

            var sendGridConfiguration = configuration.GetSection("SendGrid")?.Get<Model.Settings.SendGridConfiguration>();
            if (!string.IsNullOrEmpty(sendGridConfiguration?.MailerApiKey))
            {
                ret.EmailConfigured = true;
            }
            return ret;
        }
    }
}
