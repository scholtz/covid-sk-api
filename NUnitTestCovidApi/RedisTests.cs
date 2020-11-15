//#define DoRedisTests

using System;
using System.Collections.Generic;
using System.Text;

namespace NUnitTestCovidApi
{
#if DoRedisTests
    public class RedisTests : Tests
    {
        public override string AppSettings { get; set; } = "redis-appsettings.json";
    }
#endif
}
