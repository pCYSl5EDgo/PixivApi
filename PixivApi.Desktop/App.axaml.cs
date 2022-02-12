using Avalonia.Controls.ApplicationLifetimes;

namespace PixivApi.Desktop;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(DependencyStore.Instance?.HttpClient, DependencyStore.Instance?.ConfigSettings),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
