// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web.Http;
    using MediaLibrary.Storage;

    [RoutePrefix("searches")]
    public class SavedSearchesController : ApiController
    {
        private readonly MediaIndex index;

        public SavedSearchesController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("")]
        [HttpGet]
        public Task<List<SavedSearch>> List() => this.index.GetAllSavedSearches();
    }
}
