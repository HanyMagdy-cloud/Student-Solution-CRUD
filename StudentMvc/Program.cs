var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Read API base URL from config (Development vs Production)
var apiBase = builder.Configuration["StudentApi:BaseUrl"] ?? "http://studentapi:8080/";
builder.Services.AddHttpClient("StudentApi", client =>
{
    client.BaseAddress = new Uri(apiBase);
});

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
