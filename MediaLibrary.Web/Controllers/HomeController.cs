// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Web.Http;
    using MediaLibrary.Web.Hosting;

    public class HomeController : ApiController
    {
        public IHttpActionResult Get(string view)
        {
            return new ViewResult(view);
        }
    }
}
