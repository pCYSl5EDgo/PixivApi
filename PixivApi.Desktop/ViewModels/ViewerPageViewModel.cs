namespace PixivApi.Desktop.ViewModels;

public class ViewerPageViewModel : ViewModelBase, ReactiveUI.IRoutableViewModel
{
    public ViewerPageViewModel(ReactiveUI.IScreen hostScreen, string url)
    {
        HostScreen = hostScreen;
        UrlPathSegment = url;
    }

    public string UrlPathSegment { get; }

    public ReactiveUI.IScreen HostScreen { get; }
}
