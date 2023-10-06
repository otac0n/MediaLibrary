// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using MediaLibrary.Storage;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("files/{id}/people")]
    public class FilePeopleController : ControllerBase
    {
        private readonly MediaIndex index;

        public FilePeopleController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("{personId}")]
        [HttpPut]
        public async Task<ActionResult> AddTag(string id, int personId)
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

            await this.index.AddHashPerson(new HashPerson(id, personId)).ConfigureAwait(true);
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

            return this.Ok(result.People.Select(p => p.PersonId));
        }

        [Route("{tag}")]
        [HttpDelete]
        public async Task<ActionResult> RemoveTag(string id, int personId)
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

            await this.index.RemoveHashPerson(new HashPerson(id, personId)).ConfigureAwait(true);
            return this.Ok(new { });
        }
    }
}
