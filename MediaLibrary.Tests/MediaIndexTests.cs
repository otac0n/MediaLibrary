// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Tests
{
    using System.IO;
    using Xunit;

    public class MediaIndexTests
    {
        [Fact]
        public void Who_When_What()
        {
            var testIndexPath = Path.Combine(Path.GetTempPath(), "TestDatabase.db");
            var subject = new MediaIndex(testIndexPath);
        }
    }
}
