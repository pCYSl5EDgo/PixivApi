namespace PixivApi.Site;

public class Api
{
    private readonly ConfigSettings configSettings;
    private readonly HttpClient client;

    public Api(ConfigSettings configSettings, HttpClient client)
    {
        this.configSettings = configSettings;
        this.client = client;
    }
}
