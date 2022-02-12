namespace PixivApi.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, ReactiveUI.IScreen, IDisposable
{
    public ReactiveUI.RoutingState Router { get; } = new();

    public ReactiveCommand GoToSearch { get; }

    public ReactiveCommand<string> GoToViewer { get; }

    private readonly IDisposable gotoSearchAction;
    private readonly IDisposable gotoViewerAction;

#if DEBUG
    public MainWindowViewModel(HttpClient? httpClient, ConfigSettings? configSettings)
#else
    public MainWindowViewModel(HttpClient httpClient, ConfigSettings configSettings)
#endif
    {
        Splat.Locator.CurrentMutable.Register(() => new ViewerPage(), typeof(ReactiveUI.IViewFor<ViewerPageViewModel>));
        Splat.Locator.CurrentMutable.Register(() => new SearchPage(), typeof(ReactiveUI.IViewFor<SearchPageViewModel>));
        
        GoToSearch = new ReactiveCommand(Router.Navigate.CanExecute);
        gotoSearchAction = GoToSearch.Subscribe(() =>
        {
            Router.Navigate.Execute(new SearchPageViewModel(this, httpClient, configSettings));
        });

        GoToViewer = new ReactiveCommand<string>(Router.Navigate.CanExecute);
        gotoViewerAction = GoToViewer.Subscribe(url =>
        {
            Router.Navigate.Execute(new ViewerPageViewModel(this, url));
        });
    }

    public void Dispose()
    {
        gotoSearchAction.Dispose();
        GoToSearch.Dispose();
        gotoViewerAction.Dispose();
        GoToViewer.Dispose();
    }
}
