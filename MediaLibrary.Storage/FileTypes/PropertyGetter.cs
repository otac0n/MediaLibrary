// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.FileTypes
{
    internal delegate bool PropertyGetter<TSource, TValue>(TSource image, out TValue value);
}
