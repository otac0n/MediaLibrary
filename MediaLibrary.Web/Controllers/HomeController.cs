// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using MediaLibrary.Web.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.StaticFiles;

    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        [Route("{*view}")]
        public ActionResult Get(string view = null)
        {
            view = view ?? "index.html";

            if (!new FileExtensionContentTypeProvider().TryGetContentType(view, out var mimeMapping))
            {
                mimeMapping = "application/octet-stream";
            }

            var stream = StaticContent.GetContent(view);
            if (stream == null)
            {
                return this.NotFound();
            }

            return new FileStreamResult(stream, mimeMapping);
        }
    }
}
