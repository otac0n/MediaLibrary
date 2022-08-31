using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using MediaLibrary.App.Properties;
using MediaLibrary.Storage;
using MediaLibrary.Storage.Search;
using ReactiveUI;

namespace MediaLibrary.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string searchText = "";
        private IList<SearchResult> searchResults = Array.Empty<SearchResult>();
        private MediaIndex index;
        private int searchVersion;
        private Task initializeTask;

        public MainWindowViewModel(MediaIndex index)
        {
            this.index = index ?? throw new ArgumentNullException(nameof(index));
            this.ToggleShowPreview = ReactiveCommand.Create(() => this.ShowPreview = !this.ShowPreview);
            this.ToggleDefaultMute = ReactiveCommand.Create(() => this.DefaultMute = !this.DefaultMute);
            this.Search = ReactiveCommand.Create((string value) => { this.SearchText = value; });

            this.initializeTask = this.Initialize();
        }

        public ReactiveCommand<Unit, bool> ToggleShowPreview { get; }

        public ReactiveCommand<Unit, bool> ToggleDefaultMute { get; }

        public ReactiveCommand<string, Unit> Search { get; }

        public bool ShowPreview
        {
            get => Settings.Default.ShowPreview;
            set
            {
                this.RaisePropertyChanging();
                Settings.Default.ShowPreview = value;
                this.RaisePropertyChanged();
                this.Save();
            }
        }

        public bool DefaultMute
        {
            get => Settings.Default.DefaultMute;
            set
            {
                this.RaisePropertyChanging();
                Settings.Default.DefaultMute = value;
                this.RaisePropertyChanged();
                this.Save();
            }
        }

        public string SearchText
        {
            get => this.searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref this.searchText, value);
                this.PerformSearch();
            }
        }

        public IList<SearchResult> SearchResults
        {
            get => this.searchResults;
            private set => this.RaiseAndSetIfChanged(ref this.searchResults, value);
        }

        public TimeSpan AutoSearchDelay
        {
            get
            {
                const double KeyboardDelayIncrementSeconds = 0.250;
                var systemKeyboardDelay = TimeSpan.FromSeconds(Math.Max(0.0, Math.Min(9.0, SystemInformation.KeyboardDelay)) * KeyboardDelayIncrementSeconds + KeyboardDelayIncrementSeconds);
                var value = TimeSpan.FromSeconds(Math.Max(systemKeyboardDelay.TotalSeconds, Math.Min(Settings.Default.AutoSearchDelay.TotalSeconds, systemKeyboardDelay.TotalSeconds * 4)));
                return value;
            }
        }

        private async Task Initialize()
        {
            await this.index.Initialize().ConfigureAwait(true);
            await this.index.Rescan(); // TODO: this.TrackTask();
        }

        private async Task PerformSearch()
        {
            var searchText = this.SearchText;
            // TODO: Track selected objects.
            ////var selectedHashes = new HashSet<string>(this.listView.SelectedResults.Select(r => r.Hash));
            var searchVersion = Interlocked.Increment(ref this.searchVersion);

            // TODO: Support skipping the delay for saved searches.
            await Task.Delay(this.AutoSearchDelay).ConfigureAwait(true);
            if (this.searchVersion != searchVersion)
            {
                return;
            }

            IList<SearchResult> data;
            try
            {
                data = await this.index.SearchIndex(searchText).ConfigureAwait(true);
            }
            catch
            {
                data = Array.Empty<SearchResult>();
            }

            if (this.searchVersion == searchVersion)
            {
                this.SearchResults = data;
                ////this.listView.SelectObjects(data.Where(d => selectedHashes.Contains(d.Hash)).ToList());
            }
        }

        private void Save()
        {
            // TODO: Throttle.
            Settings.Default.Save();
        }
    }
}
