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
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using HtmlAgilityPack;
    using NeoSmart.AsyncLock;

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
        private static readonly AsyncLock syncRoot = new AsyncLock();

        public static async Task<Image> GetFavicon(Uri baseUri)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            Image image;
            using (await syncRoot.LockAsync().ConfigureAwait(false))
            {
                if (IconCache.TryGetValue(baseUri.ToString(), out image))
                {
                    return (Image)image?.Clone();
                }

                string key;
                using (var sha = new SHA256Managed())
                {
                    key = string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(baseUri.ToString())).Select(h => $"{h:x2}"));
                }

                var tempPath = Path.Combine(Environment.ExpandEnvironmentVariables("%temp%"), "iconcache", key);

                if (File.Exists(tempPath))
                {
                    try
                    {
                        using (var stream = File.OpenRead(tempPath))
                        using (var streamCopy = new MemoryStream())
                        {
                            await stream.CopyToAsync(streamCopy).ConfigureAwait(false);
                            streamCopy.Seek(0, SeekOrigin.Begin);
                            image = Image.FromStream(streamCopy);
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }

                if (image == null)
                {
                    using (var client = new HttpClient())
                    {
                        var uris = await GetFaviconUris(client, baseUri).ConfigureAwait(false);
                        foreach (var faviconUri in uris)
                        {
                            try
                            {
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

                                            break;
                                        }
                                    }
                                }
                            }
                            catch (HttpRequestException)
                            {
                                continue;
                            }
                        }
                    }
                }

                IconCache[baseUri.ToString()] = image;
                if (image != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
                    image.Save(tempPath);
                }
            }

            return (Image)image?.Clone();
        }

        private static List<Uri> FindFaviconLinks(HtmlDocument doc, Uri baseUri)
        {
            UpdateBasUri(doc, ref baseUri);
            var links = doc.DocumentNode.SelectNodes("//link[@href][@rel='icon' or @rel='shortcut icon' or @rel='apple-touch-icon']") ?? Enumerable.Empty<HtmlNode>();
            return links.Select(l => GetBaseRelativeHref(l, baseUri)).ToList();
        }

        private static Uri GetBaseRelativeHref(HtmlNode node, Uri baseUri)
        {
            var relative = node.Attributes["href"].DeEntitizeValue;
            return new Uri(baseUri, relative);
        }

        private static async Task<List<Uri>> GetFaviconUris(HttpClient client, Uri baseUri)
        {
            var seen = new HashSet<Uri>();
            var uris = new List<Uri>();

            var doc = await TryGetHtmlDocument(client, baseUri).ConfigureAwait(false);
            if (doc != null)
            {
                var links = FindFaviconLinks(doc, baseUri);
                foreach (var link in links.Where(seen.Add))
                {
                    uris.Add(link);
                }
            }

            foreach (var known in FaviconPriority.Select(f => new Uri(baseUri, "/" + f)).Where(seen.Add))
            {
                uris.Add(known);
            }

            return uris;
        }

        private static Encoding TryGetEncoding(string charSet)
        {
            try
            {
                return Encoding.GetEncoding(charSet);
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

        private static async Task<HtmlDocument> TryGetHtmlDocument(HttpClient client, Uri baseUri)
        {
            try
            {
                using (var response = await client.GetAsync(baseUri).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var doc = new HtmlDocument();
                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            var encoding = TryGetEncoding(response.Content.Headers.ContentType.CharSet);
                            if (encoding == null)
                            {
                                doc.Load(stream, detectEncodingFromByteOrderMarks: true);
                            }
                            else
                            {
                                doc.Load(stream, encoding);
                            }
                        }

                        return doc;
                    }
                }
            }
            catch (HttpRequestException)
            {
            }

            return null;
        }

        private static void UpdateBasUri(HtmlDocument doc, ref Uri baseUri)
        {
            var baseNode = doc.DocumentNode.SelectSingleNode("/html/head/base[@href]");
            if (baseNode != null)
            {
                baseUri = GetBaseRelativeHref(baseNode, baseUri);
            }
        }
    }
}
