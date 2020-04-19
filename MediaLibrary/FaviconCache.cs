// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class FaviconCache
    {
        private static readonly Dictionary<string, Image> IconCache = new Dictionary<string, Image>(StringComparer.InvariantCultureIgnoreCase);

        public static async Task<Image> GetFavicon(Uri baseUri)
        {
            var faviconUri = new Uri(baseUri, "/favicon.ico");

            Image image;
            lock (IconCache)
            {
                if (IconCache.TryGetValue(faviconUri.ToString(), out image))
                {
                    return image;
                }
            }

            try
            {
                using (var client = new HttpClient())
                using (var response = await client.GetAsync(faviconUri).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound ||
                        response.StatusCode == HttpStatusCode.Gone)
                    {
                        image = null;
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var streamCopy = new MemoryStream())
                        {
                            await stream.CopyToAsync(streamCopy).ConfigureAwait(false);

                            try
                            {
                                streamCopy.Seek(0, SeekOrigin.Begin);
                                image = new Icon(streamCopy).ToBitmap();
                            }
                            catch (ArgumentException)
                            {
                                streamCopy.Seek(0, SeekOrigin.Begin);
                                image = Image.FromStream(streamCopy);
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                return null;
            }

            lock (IconCache)
            {
                return IconCache[faviconUri.ToString()] = image;
            }
        }
    }
}
