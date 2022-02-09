namespace PixivApi.Desktop.Views
{
    public partial class SearchPage : ReactiveUserControl<SearchPageViewModel>
    {
        public SearchPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            ReactiveUI.ViewForMixins.WhenActivated(this, disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}
