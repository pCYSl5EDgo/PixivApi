using System.Reactive.Linq;

namespace PixivApi.Desktop.ViewModels;

public sealed class SearchPageViewModel : ViewModelBase, ReactiveUI.IRoutableViewModel, IDisposable
{
#if DEBUG
    public SearchPageViewModel(ReactiveUI.IScreen hostScreen, HttpClient? httpClient, ConfigSettings? configSettings)
#else
    public SearchPageViewModel(ReactiveUI.IScreen hostScreen, HttpClient httpClient, ConfigSettings configSettings)
#endif
    {
        HostScreen = hostScreen;
        this.httpClient = httpClient;
        this.configSettings = configSettings;
        SearchText = new(string.Empty);
        IsDescending = new(true);
        OrderIndex = new(0);

        BookmarkFilter = new(null);
        BookmarkFilterText = new ReadOnlyReactivePropertySlim<string>(BookmarkFilter.Select(x => x == null ? "Bookmark Filter" : x.Value ? "Only Bookmark" : "Except Bookmark"), "Bookmark Filter");

        FollowerFilter = new(null);
        FollowerFilterText = new ReadOnlyReactivePropertySlim<string>(FollowerFilter.Select(x => x == null ? "Follower Filter" : x.Value ? "Only Follower" : "Except Follower"), "Follower Filter");

        R18Filter = new(null);
        R18FilterText = new ReadOnlyReactivePropertySlim<string>(R18Filter.Select(x => x == null ? "R18 Filter" : x.Value ? "Only R18" : "Except R18"), "R18 Filter");

        TotalBookmarkMin = new(string.Empty);
        TotalBookmarkMax = new(string.Empty);

        TotalBookmarkMinObservable = TotalBookmarkMin.Select(x => ulong.TryParse(x, out var result) ? result : 0UL);
        TotalBookmarkMaxObservable = TotalBookmarkMax.Select(x => ulong.TryParse(x, out var result) ? result : ulong.MaxValue);

        networkSearchAsyncModelObservable = new(null);
        NetSearch = new AsyncReactiveCommand(networkSearchAsyncModelObservable.Select(x => x is null));
        networkSearchOperation = NetSearch.Subscribe(SearchAsync);

        Since = new(default(DateOnly?));
        Until = new(default(DateOnly?));
    }

    public string UrlPathSegment => "search";

    public ReactiveUI.IScreen HostScreen { get; }

    public ReactivePropertySlim<string> SearchText { get; }

    public ReactivePropertySlim<bool> IsDescending { get; }

    public ReactivePropertySlim<int> OrderIndex { get; }

    public ReactivePropertySlim<bool?> BookmarkFilter { get; }

    public ReadOnlyReactivePropertySlim<string> BookmarkFilterText { get; }

    public ReactivePropertySlim<bool?> FollowerFilter { get; }

    public ReadOnlyReactivePropertySlim<string> FollowerFilterText { get; }

    public ReactivePropertySlim<bool?> R18Filter { get; }

    public ReadOnlyReactivePropertySlim<string> R18FilterText { get; }

    public ReactiveProperty<string> TotalBookmarkMin { get; }

    public ReactiveProperty<string> TotalBookmarkMax { get; }

    public ReactiveProperty<DateOnly?> Since { get; }
    
    public ReactiveProperty<DateOnly?> Until { get; }

    public IObservable<ulong> TotalBookmarkMinObservable { get; }

    public IObservable<ulong> TotalBookmarkMaxObservable { get; }

    public AsyncReactiveCommand NetSearch { get; }

    private readonly ReactivePropertySlim<NetworkSearchAsyncModel?> networkSearchAsyncModelObservable;

    private readonly IDisposable networkSearchOperation;
#if DEBUG
    private readonly HttpClient? httpClient;
    private readonly ConfigSettings? configSettings;
#else
    private readonly HttpClient httpClient;
    private readonly ConfigSettings configSettings;
#endif

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText.Value))
        {
            return;
        }

        var value = new NetworkSearchAsyncModel(SearchText.Value, R18Filter.Value, Since.Value, Until.Value);
        networkSearchAsyncModelObservable.Value = value;
        await value.StartSearchAsync(httpClient, configSettings, default).ConfigureAwait(false);
    }

    public void Dispose()
    {
        SearchText.Dispose();
        IsDescending.Dispose();
        OrderIndex.Dispose();
        BookmarkFilter.Dispose();
        BookmarkFilterText.Dispose();
        FollowerFilter.Dispose();
        FollowerFilterText.Dispose();
        R18Filter.Dispose();
        R18FilterText.Dispose();
        Since.Dispose();
        Until.Dispose();
        TotalBookmarkMin.Dispose();
        TotalBookmarkMax.Dispose();
        NetSearch.Dispose();
        networkSearchAsyncModelObservable.Dispose();
        networkSearchOperation.Dispose();
    }
}
