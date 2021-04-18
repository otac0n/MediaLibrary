// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Hosting
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;

    public class ViewResult : IHttpActionResult
    {
        public ViewResult(string view)
        {
            this.View = view ?? "index.html";
        }

        public string View { get; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var mimeMapping = MimeMapping.GetMimeMapping(this.View);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(StaticContent.GetContent(this.View));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeMapping);
            return Task.FromResult(response);
        }
    }
}
