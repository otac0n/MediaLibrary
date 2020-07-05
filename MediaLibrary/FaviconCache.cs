// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class FaviconCache
    {
        private static readonly ImmutableQueue<string> FaviconPriority = ImmutableQueue.CreateRange(new[]
        {
            // https://www.emergeinteractive.com/insights/detail/the-essentials-of-favicons/
            "favicon-196.png", // Chrome for Android home screen icon
            "favicon-192.png", // Google Developer Web App Manifest Recommendation
            "favicon-180.png", // iPhone Retina
            "favicon-167.png", // iPad Retina touch icon (change for iOS 10: up from 152×152, not in action. iOS 10 will use 152×152)
            "favicon-152.png", // iPad touch icon (Change for iOS 7: up from 144×144)
            "favicon-128.png", // Chrome Web Store icon & Small Windows 8 Star Screen Icon*
            "favicon-32.png", //  Standard for most desktop browsers
            "favicon.ico", // Fallback
        });

        private static readonly Dictionary<string, Image> IconCache = new Dictionary<string, Image>(StringComparer.InvariantCultureIgnoreCase);

        public static async Task<Image> GetFavicon(Uri baseUri)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            Image image;
            lock (IconCache)
            {
                if (IconCache.TryGetValue(baseUri.ToString(), out image))
                {
                    return image;
                }
            }

            foreach (var faviconUri in FaviconPriority.Select(f => new Uri(baseUri, "/" + f)))
            {
                try
                {
                    using (var client = new HttpClient())
                    using (var response = await client.GetAsync(faviconUri).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound ||
                            response.StatusCode == HttpStatusCode.Gone)
                        {
                            continue;
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
                                    using (var icon = new Icon(streamCopy))
                                    {
                                        image = icon.ToBitmap();
                                    }
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
                    continue;
                }
            }

            if (image != null)
            {
                lock (IconCache)
                {
                    IconCache[baseUri.ToString()] = image;
                }
            }

            return image;
        }
    }
}
