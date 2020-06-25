// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Http
{
    using System.Threading.Tasks;
    using System.Web.Http;

    public class HomeController : ApiController
    {
        [Route("")]
        [HttpGet]
        public async Task<IHttpActionResult> Get()
        {
            return new ViewResult("Index");
        }
    }
}
