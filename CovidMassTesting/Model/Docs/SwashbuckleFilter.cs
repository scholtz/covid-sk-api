using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Docs
{
    public class SwashbuckleFilter : IDocumentFilter

    {

        private readonly string _swaggerDocHost;


        public SwashbuckleFilter(IHttpContextAccessor httpContextAccessor)

        {

            var host = httpContextAccessor.HttpContext.Request.Host.Value;

            var scheme = httpContextAccessor.HttpContext.Request.Scheme;

            _swaggerDocHost = $"https://{host}";

        }


        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)

        {
            swaggerDoc.Servers.Clear();
            swaggerDoc.Servers.Add(new OpenApiServer { Url = _swaggerDocHost });

        }

    }
}
