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
        /// Get version
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="start"></param>
        /// <param name="dllVersion"></param>
        /// <returns></returns>
        public static Version GetVersion(string instanceId, DateTimeOffset start, string dllVersion)
        {
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
                        ret.BuildNumber = versionData[0].Substring(pos + 1).Trim();
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

            return ret;
        }
    }
}
