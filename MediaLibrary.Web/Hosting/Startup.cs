// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Hosting
{
    using System;
    using System.Net;
    using System.Net.Http.Formatting;
    using System.Text;
    using System.Text.RegularExpressions;
    using MediaLibrary.Storage;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    public static class Startup
    {
        public static void Build(IWebHostBuilder builder, string baseUri, MediaIndex index)
        {
            builder.UseUrls(baseUri);
            builder
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                    services
                        .AddSingleton(index)
                        .AddResponseCompression();
                })
                .Configure(app =>
                {
                    app
                        .UseResponseCompression()
                        .UseRouting()
                        .UseEndpoints(endpoints =>
                            endpoints
                                .MapControllers());
                });
        }
    }
}
