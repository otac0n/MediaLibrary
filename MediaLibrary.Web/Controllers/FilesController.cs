// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;
    using Microsoft.AspNetCore.Mvc;
    using TaggingLibrary;

    [ApiController]
    [Route("files")]
    public class FilesController : ControllerBase
    {
        private readonly MediaIndex index;

        public FilesController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<ActionResult> Get(string id)
        {
            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}", excludeHidden: false).ConfigureAwait(true)).SingleOrDefault();
            var path = result?.Paths?.Select(PathEncoder.ExtendPath)?.FirstOrDefault(p => System.IO.File.Exists(p));

            if (path == null)
            {
                return this.NotFound();
            }

            return new PhysicalFileResult(path, result.FileType);
        }

        [Route("")]
        [HttpGet]
        public async Task<IEnumerable<SearchResult>> Search(string q = null)
        {
            ControllerUtilities.FixSlashes(ref q);

            var results = await this.index.SearchIndex(q).ConfigureAwait(true);
            return results.OrderBy(r => r.Rating, Rating.Comparer).Take(1000);
        }
    }
}
