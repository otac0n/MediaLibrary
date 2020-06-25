// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Http
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    public class ViewResult : IHttpActionResult
    {
        private static readonly Assembly SourceAssembly = Assembly.GetExecutingAssembly();

        public ViewResult(string view)
        {
            if (string.IsNullOrWhiteSpace(view))
            {
                throw new ArgumentNullException(nameof(view));
            }

            this.View = view;
        }

        public string View { get; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(SourceAssembly.GetManifestResourceStream($"MediaLibrary.Http.Views.{this.View}.html"));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return Task.FromResult(response);
        }
    }
}
