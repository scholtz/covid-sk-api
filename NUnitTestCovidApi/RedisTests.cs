//#define DoRedisTests

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NUnitTestCovidApi
{
#if DoRedisTests
    public class RedisTests : Tests
    {
        public override string AppSettings { get; set; } = "redis-appsettings.json";

        1
        [Test]
        public override void TestVersion()
        {
            DropDatabase();

            using var web = new MockWebApp(AppSettings);
            var client = web.CreateClient();
            var request = CheckVersion(client);
            Assert.AreEqual(HttpStatusCode.OK, request.StatusCode, request.Content.ReadAsStringAsync().Result);

            var version = Newtonsoft.Json.JsonConvert.DeserializeObject<CovidMassTesting.Model.Version>(request.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(false, version.SMSConfigured);
            Assert.AreEqual(false, version.EmailConfigured);
            Assert.AreEqual(true, version.RedisConfigured);
            Assert.IsNotNull(version.StorageTest);
        }
    }

#endif
}
