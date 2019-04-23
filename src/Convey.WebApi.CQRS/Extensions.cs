using System;
using System.Collections.Generic;
using System.Linq;
using Convey.WebApi.CQRS.Builders;
using Convey.WebApi.CQRS.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Convey.WebApi.CQRS
{
    public static class Extensions
    {
        public static IApplicationBuilder UseDispatcherEndpoints(this IApplicationBuilder app,
            Action<IDispatcherEndpointsBuilder> builder)
        {
            var definitions = app.ApplicationServices.GetService<WebApiEndpointDefinitions>();

            return app.UseRouter(router =>
                builder(new DispatcherEndpointsBuilder(new EndpointsBuilder(router, definitions))));
        }

        public static IDispatcherEndpointsBuilder Dispatch(this IEndpointsBuilder endpoints,
            Func<IDispatcherEndpointsBuilder, IDispatcherEndpointsBuilder> builder)
            => builder(new DispatcherEndpointsBuilder(endpoints));

        public static IApplicationBuilder UsePublicMessages(this IApplicationBuilder app,
            string endpoint = "/_messages", IEnumerable<Type> assemblyTypes = null,
            bool attributeRequired = true, Type attributeType = null)
            => app.UseMiddleware<PublicMessagesMiddleware>(string.IsNullOrWhiteSpace(endpoint) ? "/_messages" :
                endpoint.StartsWith("/") ? endpoint : $"/{endpoint}",
                assemblyTypes ?? Enumerable.Empty<Type>(), attributeRequired,
                attributeType ?? typeof(PublicMessageAttribute));
    }
}