// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Http
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    public class FileResult : IHttpActionResult
    {
        public FileResult(string path, string contentType, bool attachment = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            this.FilePath = path;
            this.ContentType = contentType;
            this.Attachment = attachment;
        }

        public bool Attachment { get; }

        public string ContentType { get; }

        public string FilePath { get; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(File.OpenRead(this.FilePath));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(this.ContentType);

            if (this.Attachment)
            {
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = Path.GetFileName(this.FilePath),
                };
            }

            return Task.FromResult(response);
        }
    }
}
