namespace Eevee.Sleep.Bot.Modules;

public static class HttpModule {
    public static readonly HttpClient Client = new() {
        Timeout = TimeSpan.FromSeconds(120),
    };
}