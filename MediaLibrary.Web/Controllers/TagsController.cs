// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using MediaLibrary.Storage;
    using Microsoft.AspNetCore.Mvc;
    using TaggingLibrary;

    [ApiController]
    [Route("tags")]
    public class TagsController : ControllerBase
    {
        private readonly MediaIndex index;

        public TagsController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("{tag}")]
        [HttpGet]
        public ActionResult Get(string tag)
        {
            var tagInfo = this.index.TagEngine[tag];

            if (tagInfo.Tag != tag)
            {
                return this.RedirectToRoute(new { Controller = "Tags", Action = "Get", tagInfo.Tag });
            }

            return this.Ok(tagInfo);
        }

        [Route("")]
        [HttpGet]
        public async Task<ActionResult> List()
        {
            var engine = this.index.TagEngine;
            var rawTags = await this.index.GetAllHashTags().ConfigureAwait(true);
            var tags = new HashSet<string>(engine.GetKnownTags().Concat(rawTags).Select(engine.Rename));

            return this.Ok(tags.OrderBy(t => t).Select(t => engine[t]));
        }
    }
}
