namespace PixivApi.Desktop;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        Splat.Locator.CurrentMutable.Register(() => new SearchPage(), typeof(ReactiveUI.IViewFor<SearchPageViewModel>));

        return AppBuilder.Configure<App>()
                   .UsePlatformDetect()
                   .LogToTrace()
                   .UseReactiveUI();
    }
}
