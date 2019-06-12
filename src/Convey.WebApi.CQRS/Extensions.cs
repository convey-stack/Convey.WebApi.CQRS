using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Convey.CQRS.Commands;
using Convey.CQRS.Queries;
using Convey.WebApi.CQRS.Builders;
using Convey.WebApi.CQRS.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        public static IApplicationBuilder UsePublicContracts(this IApplicationBuilder app,
            string endpoint = "/_contracts", IEnumerable<Type> assemblyTypes = null,
            bool attributeRequired = true, Type attributeType = null)
            => app.UseMiddleware<PublicContractsMiddleware>(string.IsNullOrWhiteSpace(endpoint) ? "/_contracts" :
                endpoint.StartsWith("/") ? endpoint : $"/{endpoint}",
                assemblyTypes ?? Enumerable.Empty<Type>(), attributeRequired,
                attributeType ?? typeof(PublicContractAttribute));

        public static Task SendAsync<T>(this HttpContext context, T command) where T : class, ICommand
            => context.RequestServices.GetService<ICommandDispatcher>().SendAsync(command);

        public static Task<TResult> QueryAsync<TResult>(this HttpContext context, IQuery<TResult> query)
            => context.RequestServices.GetService<IQueryDispatcher>().QueryAsync(query);

        public static Task<TResult> QueryAsync<TQuery, TResult>(this HttpContext context, TQuery query)
            where TQuery : class, IQuery<TResult>
            => context.RequestServices.GetService<IQueryDispatcher>().QueryAsync<TQuery, TResult>(query);
    }
}