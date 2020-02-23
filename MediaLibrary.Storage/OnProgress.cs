// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;

    public static class OnProgress
    {
        public static IProgress<T> Do<T>(Action<T> action) => new ActionProgress<T>(action);

        private class ActionProgress<T> : IProgress<T>
        {
            private readonly Action<T> action;

            public ActionProgress(Action<T> action)
            {
                this.action = action;
            }

            public void Report(T value) => this.action(value);
        }
    }
}
