namespace PixivApi.Desktop.ViewModels;

public class SearchPageViewModel : ViewModelBase, ReactiveUI.IRoutableViewModel
{
    public SearchPageViewModel(ReactiveUI.IScreen hostScreen)
    {
        HostScreen = hostScreen;
        ArtworkItems = new(Avalonia.Threading.AvaloniaScheduler.Instance);
    }

    public string? UrlPathSegment => "search";

    public ReactiveUI.IScreen HostScreen { get; }

    public ReactiveCollection<ArtworkDatabaseInfo> ArtworkItems { get; }
}
