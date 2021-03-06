// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web.Http;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;
    using MediaLibrary.Web.Hosting;

    [RoutePrefix("files")]
    public class FilesController : ApiController
    {
        private readonly MediaIndex index;

        public FilesController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<IHttpActionResult> Get(string id)
        {
            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}", excludeHidden: false).ConfigureAwait(true)).SingleOrDefault();
            var path = result?.Paths?.Select(PathEncoder.ExtendPath)?.FirstOrDefault(p => File.Exists(p));

            if (path == null)
            {
                return this.NotFound();
            }

            return new FileResult(path, result.FileType, this.Request.Headers.Range);
        }

        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<SearchResult>> Search(string q = null)
        {
            var results = await this.index.SearchIndex(q).ConfigureAwait(true);
            return results.OrderBy(r => r.Paths.OrderBy(p => p, PathComparer.Instance).FirstOrDefault(), PathComparer.Instance).Take(1000);
        }
    }
}
