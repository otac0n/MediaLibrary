// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MediaLibrary.Storage;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("searches")]
    public class SavedSearchesController : ControllerBase
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
