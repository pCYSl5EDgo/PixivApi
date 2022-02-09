namespace PixivApi.Desktop.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        ReactiveUI.ViewForMixins.WhenActivated(this, disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
