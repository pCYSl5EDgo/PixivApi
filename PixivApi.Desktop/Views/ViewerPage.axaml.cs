namespace PixivApi.Desktop.Views;

public partial class ViewerPage : ReactiveUserControl<ViewerPageViewModel>
{
    public ViewerPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        ReactiveUI.ViewForMixins.WhenActivated(this, disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
