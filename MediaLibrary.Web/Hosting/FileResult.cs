// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Hosting
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
        public FileResult(string path, string contentType, RangeHeaderValue range = null)
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
            this.Range = range;
        }

        public string ContentType { get; }

        public string FilePath { get; }

        public RangeHeaderValue Range { get; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            FileStream stream = null;
            try
            {
                stream = File.OpenRead(this.FilePath);

                HttpResponseMessage response;
                if (this.Range != null)
                {
                    // Return part of the video
                    response = new HttpResponseMessage(HttpStatusCode.PartialContent)
                    {
                        Content = new ByteRangeStreamContent(stream, this.Range, this.ContentType),
                    };
                }
                else
                {
                    // Return complete video
                    response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(stream),
                    };

                    response.Content.Headers.ContentType = new MediaTypeHeaderValue(this.ContentType);
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = Path.GetFileName(this.FilePath),
                    };
                }

                var result = Task.FromResult(response);
                stream = null;
                return result;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }
    }
}
