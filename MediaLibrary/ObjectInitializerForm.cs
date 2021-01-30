// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.FormsClerk;
    using MediaLibrary.FormsClerk.Editors;

    public partial class ObjectInitializerForm<T> : Form
    {
        private static readonly bool IsValueType = typeof(T).IsValueType;
        private T currentState;
        private IInitializer[] initializers;
        private Control rootEditor;
        private Scope scope;

        public ObjectInitializerForm(IInitializer[] initializers = null, T startingState = default)
        {
            this.InitializeComponent();
            this.initializers = (initializers?.AsEnumerable() ?? typeof(T).GetPublicInitializers()).ToArray();
            this.scope = new Scope(string.Empty, this);
            this.currentState = startingState;
            this.UpdateEditor();
        }

        /// <summary>
        /// Raised when the currently selected state changes.
        /// </summary>
        public event EventHandler<CurrentStateChangedEventArgs> CurrentStateChanged;

        /// <summary>
        /// Gets the currently selected state.
        /// </summary>
        public T CurrentState
        {
            get
            {
                return this.currentState;
            }

            private set
            {
                if (IsValueType || !object.ReferenceEquals(this.currentState, value))
                {
                    this.currentState = value;
                    this.OnCurrentStateChanged();
                }
            }
        }

        protected void OnCurrentStateChanged()
        {
            var state = this.CurrentState;
            this.CurrentStateChanged?.Invoke(this, new CurrentStateChangedEventArgs(state));
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }

        private void FinishButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Hide();
        }

        private void UpdateEditor()
        {
            ObjectGraphEditor.Instance.Update(
                this.rootEditor,
                this.scope,
                new Parameter("currentState", typeof(T)),
                this.initializers,
                this.CurrentState,
                out var errorControl,
                null,
                this.errorProvider.SetError,
                (value, valid) =>
                {
                    (this.CurrentState as IDisposable)?.Dispose();
                    this.CurrentState = valid ? (T)value : default;
                },
                update: (oldControl, newControl) =>
                {
                    if (oldControl != null)
                    {
                        this.Controls.Remove(oldControl);
                        oldControl.Dispose();
                        this.rootEditor = oldControl = null;
                    }

                    if (newControl != null)
                    {
                        this.Controls.Add(this.rootEditor = newControl);
                    }
                });
        }

        /// <summary>
        /// <see cref="EventArgs"/> for the <see cref="CurrentStateChanged"/> event.
        /// </summary>
        public class CurrentStateChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CurrentStateChangedEventArgs"/> class.
            /// </summary>
            /// <param name="currentState">The currently selected state.</param>
            public CurrentStateChangedEventArgs(object currentState)
            {
                this.CurrentState = currentState;
            }

            /// <summary>
            /// Gets the currently selected state.
            /// </summary>
            public object CurrentState { get; }
        }
    }
}
