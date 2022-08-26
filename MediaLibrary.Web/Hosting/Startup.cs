// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Hosting
{
    using System.Net.Http.Formatting;
    using System.Text;
    using System.Web.Http;
    using MediaLibrary.Storage;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using Owin;
    using SqueezeMe;
    using Unity;
    using Unity.AspNet.WebApi;

    public class Startup
    {
        private MediaIndex index;

        public Startup(MediaIndex index)
        {
            this.index = index;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            var container = new UnityContainer();

            container.RegisterInstance(this.index);

            var config = new HttpConfiguration();

            config.DependencyResolver = new UnityHierarchicalDependencyResolver(container);

            var formatter = new JsonMediaTypeFormatter
            {
                SerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy
                        {
                            ProcessDictionaryKeys = false,
                        },
                    },
                },
            };
            formatter.SupportedEncodings.Clear();
            formatter.SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            config.Formatters.Clear();
            config.Formatters.Add(formatter);
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());

            config.MapHttpAttributeRoutes();

            appBuilder.UseCompression();
            appBuilder.UseWebApi(config);
        }
    }
}
