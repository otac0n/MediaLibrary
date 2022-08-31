using System;
using System.Reactive;
using MediaLibrary.App.Properties;
using ReactiveUI;

namespace MediaLibrary.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string searchText = "";

        public MainWindowViewModel()
        {
            this.ToggleShowPreview = ReactiveCommand.Create(() => this.ShowPreview = !this.ShowPreview);
            this.ToggleDefaultMute = ReactiveCommand.Create(() => this.DefaultMute = !this.DefaultMute);
            this.Search = ReactiveCommand.Create((string value) => { this.SearchText = value; });
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

        private void PerformSearch()
        {
        }

        private void Save()
        {
            // TODO: Throttle.
            Settings.Default.Save();
        }
    }
}
