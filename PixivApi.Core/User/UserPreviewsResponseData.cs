namespace PixivApi;

public record struct UserPreviewsResponseData(
    [property: JsonPropertyName("user_previews")] UserPreview[] UserPreviews,
    [property: JsonPropertyName("next_url")] string? NextUrl
) : INext, IArrayContainer<UserPreview>, IArrayContainer<UserDatabaseInfo>
{
    public UserPreview[] GetContainer() => UserPreviews;
    UserDatabaseInfo[] IArrayContainer<UserDatabaseInfo>.GetContainer() => UserPreviews.Length == 0 ? Array.Empty<UserDatabaseInfo>() : UserPreviews.Select(x => new UserDatabaseInfo(x)).ToArray();
}
