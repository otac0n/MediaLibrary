// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Http
{
    using System.Web.Http;

    public class HomeController : ApiController
    {
        public IHttpActionResult Get(string view)
        {
            return new ViewResult(view);
        }
    }
}
