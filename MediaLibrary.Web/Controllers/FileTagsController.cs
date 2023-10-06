// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using MediaLibrary.Storage;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("files/{id}/tags")]
    public class FileTagsController : ControllerBase
    {
        private readonly MediaIndex index;

        public FileTagsController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("{tag}")]
        [HttpPut]
        public async Task<ActionResult> AddTag(string id, string tag)
        {
            ControllerUtilities.FixSlashes(ref tag);

            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}", excludeHidden: false).ConfigureAwait(true)).SingleOrDefault();
            if (result == null)
            {
                return this.NotFound();
            }

            await this.index.AddHashTag(new HashTag(id, tag)).ConfigureAwait(true);
            return this.Ok(new { });
        }

        [Route("")]
        [HttpGet]
        public async Task<ActionResult> List(string id)
        {
            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}", excludeHidden: false).ConfigureAwait(true)).SingleOrDefault();
            if (result == null)
            {
                return this.NotFound();
            }

            return this.Ok(result.Tags);
        }

        [Route("{tag}")]
        [HttpDelete]
        public async Task<ActionResult> RemoveTag(string id, string tag)
        {
            ControllerUtilities.FixSlashes(ref tag);

            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}", excludeHidden: false).ConfigureAwait(true)).SingleOrDefault();
            if (result == null)
            {
                return this.NotFound();
            }

            await this.index.RemoveHashTag(new HashTag(id, tag)).ConfigureAwait(true);
            return this.Ok(new { });
        }
    }
}
