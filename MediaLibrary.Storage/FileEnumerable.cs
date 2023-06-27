// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Enumerates files, skipping errors due to access or IO failures.
    /// </summary>
    public static class FileEnumerable
    {
        public static IEnumerable<string> EnumerateFiles(string root, string searchPattern, SearchOption searchOption)
        {
            using (var fileEnumerator = Directory.EnumerateFiles(root, searchPattern, SearchOption.TopDirectoryOnly).GetEnumerator())
            {
                while (true)
                {
                    try
                    {
                        if (!fileEnumerator.MoveNext())
                        {
                            break;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        break;
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    yield return fileEnumerator.Current;
                }
            }

            if (searchOption == SearchOption.AllDirectories)
            {
                using (var directoryEnumerator = Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly).GetEnumerator())
                {
                    while (true)
                    {
                        try
                        {
                            if (!directoryEnumerator.MoveNext())
                            {
                                break;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            break;
                        }
                        catch (IOException)
                        {
                            break;
                        }

                        foreach (var match in EnumerateFiles(directoryEnumerator.Current, searchPattern, searchOption))
                        {
                            yield return match;
                        }
                    }
                }
            }
        }
    }
}
