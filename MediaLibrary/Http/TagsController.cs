// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Http
{
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web.Http;
    using MediaLibrary.Storage;

    [RoutePrefix("files/{id}/tags")]
    public class TagsController : ApiController
    {
        private readonly MediaIndex index;

        public TagsController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("{tag}")]
        [HttpPut]
        public async Task<IHttpActionResult> AddTag(string id, string tag)
        {
            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}").ConfigureAwait(true)).SingleOrDefault();
            if (result == null)
            {
                return this.NotFound();
            }

            await this.index.AddHashTag(new HashTag(id, tag)).ConfigureAwait(true);
            return this.Content(HttpStatusCode.OK, new { });
        }

        [Route("")]
        [HttpGet]
        public async Task<IHttpActionResult> List(string id)
        {
            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}").ConfigureAwait(true)).SingleOrDefault();
            if (result == null)
            {
                return this.NotFound();
            }

            return this.Content(HttpStatusCode.OK, result.Tags);
        }

        [Route("{tag}")]
        [HttpDelete]
        public async Task<IHttpActionResult> RemoveTag(string id, string tag)
        {
            if (!Regex.IsMatch(id, @"[0-9a-fA-F]{64}"))
            {
                return this.BadRequest();
            }

            var result = (await this.index.SearchIndex($"hash:{id}").ConfigureAwait(true)).SingleOrDefault();
            if (result == null)
            {
                return this.NotFound();
            }

            await this.index.RemoveHashTag(new HashTag(id, tag)).ConfigureAwait(true);
            return this.Content(HttpStatusCode.OK, new { });
        }
    }
}
