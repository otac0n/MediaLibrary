// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System;

    internal static class ControllerUtilities
    {
        public static void FixSlashes(ref string uriParameter)
        {
            uriParameter = uriParameter?.Replace("%2F", "/", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
