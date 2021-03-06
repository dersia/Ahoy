﻿using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Swashbuckle.SwaggerUi.Application
{
    public class SwaggerUiMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TemplateMatcher _requestMatcher;
        private readonly SwaggerUiConfig _config;
        private readonly Assembly _resourceAssembly;

        public SwaggerUiMiddleware(
            RequestDelegate next,
            SwaggerUiConfig config
        )
        {
            _next = next;
            _requestMatcher = new TemplateMatcher(TemplateParser.Parse(config.IndexPath), new RouteValueDictionary());
            _config = config;
            _resourceAssembly = GetType().GetTypeInfo().Assembly;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!RequestingSwaggerUi(httpContext.Request))
            {
                await _next(httpContext);
                return;
            }

            var template = _resourceAssembly.GetManifestResourceStream("Swashbuckle.SwaggerUi.CustomAssets.index.html");
            var content = AssignPlaceholderValuesTo(template);
            RespondWithContentHtml(httpContext.Response, content);
        }

        private bool RequestingSwaggerUi(HttpRequest request)
        {
            if (request.Method != "GET") return false;

			return _requestMatcher.TryMatch(request.Path, new RouteValueDictionary());
        }

        private Stream AssignPlaceholderValuesTo(Stream template)
        {
            var templateText = new StreamReader(template).ReadToEnd();
            var contentBuilder = new StringBuilder(templateText);
            foreach (var entry in _config.GetPlaceholderValues())
            {
                contentBuilder.Replace(entry.Key, entry.Value);
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(contentBuilder.ToString()));
        }

        private void RespondWithContentHtml(HttpResponse response, Stream content)
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            content.CopyTo(response.Body);
        }
    }
}
