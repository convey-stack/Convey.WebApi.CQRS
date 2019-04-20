using System;
using Convey.WebApi.Builders;
using Convey.WebApi.CQRS.Builders;
using Microsoft.AspNetCore.Builder;

namespace Convey.WebApi.CQRS
{
    public static class Extensions
    {
        public static IApplicationBuilder UseDispatcherEndpoints(this IApplicationBuilder app,
            Action<IDispatcherEndpointsBuilder> builder)
            => app.UseRouter(router => builder(new DispatcherEndpointsBuilder(new EndpointsBuilder(router))));

        public static IDispatcherEndpointsBuilder Dispatch(this IEndpointsBuilder endpoints,
            Func<IDispatcherEndpointsBuilder, IDispatcherEndpointsBuilder> builder)
            => builder(new DispatcherEndpointsBuilder(endpoints));
    }
}