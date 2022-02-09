namespace PixivApi;

[T4("Network", kind: Kind.Utf8)]
public partial record struct OAuthRefreshTokenPostRequestData(
    string ClientId,
    string ClientSecret,
    string Code,
    string CodeVerifier
) : ITransformAppend
{
}
