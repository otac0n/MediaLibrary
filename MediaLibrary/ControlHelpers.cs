// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    internal static class ControlHelpers
    {
        /// <summary>
        /// Extension method allowing conditional invoke usage.
        /// </summary>
        /// <param name="this">The object with which to synchronize.</param>
        /// <param name="action">The action to perform.</param>
        public static void InvokeIfRequired(this ISynchronizeInvoke @this, MethodInvoker action)
        {
            if (@this.InvokeRequired)
            {
                if (@this is Control control && control.IsDisposed)
                {
                    return;
                }

                @this.Invoke(action, Array.Empty<object>());
            }
            else
            {
                action();
            }
        }
    }
}
