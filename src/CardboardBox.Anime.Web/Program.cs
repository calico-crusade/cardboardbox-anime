using CardboardBox.Anime;
using CardboardBox.Anime.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.RegisterCba(builder.Configuration)
				.AddTransient<IOpenGraphService, OpenGraphService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/error");
}
app.UseStaticFiles();

app.UseRouting();
app.UseEndpoints(c =>
{
	c.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
	c.MapFallbackToFile("/index.html");
});
app.Run();
