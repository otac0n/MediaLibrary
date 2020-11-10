// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Http
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using MediaLibrary.Web;

    public class ViewResult : IHttpActionResult
    {
        public ViewResult(string view)
        {
            this.View = view;
        }

        public string View { get; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(StaticContent.GetContent(this.View));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return Task.FromResult(response);
        }
    }
}
