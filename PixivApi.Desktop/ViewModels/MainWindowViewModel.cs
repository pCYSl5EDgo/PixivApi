namespace PixivApi.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase, ReactiveUI.IScreen
{
    public ReactiveUI.RoutingState Router { get; } = new();

    public ReactiveUI.ReactiveCommand<Unit, ReactiveUI.IRoutableViewModel> GoToSearch { get; }

    public MainWindowViewModel()
    {
        GoToSearch = ReactiveUI.ReactiveCommand.CreateFromObservable(PrivateGoToSearch);
    }

    private IObservable<ReactiveUI.IRoutableViewModel> PrivateGoToSearch() => Router.Navigate.Execute(new SearchPageViewModel(this));
}
