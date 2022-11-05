using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
	.AddCardboardHttp()
	.AddTransient<IAnimeService, AnimeService>()
	.AddTransient<ICacheService, FakeCacheService>();

await builder.Build().RunAsync();
