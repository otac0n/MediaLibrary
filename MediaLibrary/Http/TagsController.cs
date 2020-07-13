// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http;
    using MediaLibrary.Storage;

    [RoutePrefix("tags")]
    public class TagsController : ApiController
    {
        private readonly MediaIndex index;

        public TagsController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("{tag}")]
        [HttpGet]
        public IHttpActionResult Get(string tag)
        {
            var tagInfo = this.index.TagEngine[tag];

            if (tagInfo.Tag != tag)
            {
                var uri = new UriBuilder(this.Request.RequestUri);
                uri.Path = uri.Path.Substring(0, uri.Path.LastIndexOf('/') + 1) + Uri.EscapeDataString(tagInfo.Tag);
                return this.Redirect(uri.Uri);
            }

            return this.Content(HttpStatusCode.OK, tagInfo);
        }

        [Route("")]
        [HttpGet]
        public async Task<IHttpActionResult> List()
        {
            var engine = this.index.TagEngine;
            var rawTags = await this.index.GetAllTags().ConfigureAwait(true);
            var tags = new HashSet<string>(rawTags.Select(engine.Rename).Concat(engine.GetKnownTags()));

            return this.Content(HttpStatusCode.OK, tags.OrderBy(t => t).Select(t => engine[t]));
        }
    }
}
