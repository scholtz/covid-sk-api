using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CovidMassTesting.Helpers
{
    /// <summary>
    /// IP extensions
    /// </summary>
    public static class IP
    {
        /// <summary>
        /// ip address
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static string GetIPAddress(this HttpContext httpContext)
        {
            if (httpContext == null) return "";
            try
            {
                var ip = httpContext.Connection?.RemoteIpAddress;
                var headers = httpContext.Request?.Headers;
                if (headers?.ContainsKey("X-Forwarded-For") == true)
                {
                    ip = IPAddress.Parse(headers["X-Forwarded-For"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)[0]);
                }
                return ip?.ToString() ?? "";
            }
            catch
            {
                return "";
            }

        }
    }
}
