namespace PixivApi;

[T4("Network", kind: Kind.Utf8)]
public partial record struct AccessTokenPostRequestData(
    string ClientId, 
    string ClientSecret, 
    string RefreshToken
) : ITransformAppend
{
}
