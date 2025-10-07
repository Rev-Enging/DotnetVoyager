using DotnetVoyager.WebAPI.Configuration;
using DotnetVoyager.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("StorageOptions"));

builder.Services.AddScoped<IStorageService, StorageService>();

var app = builder.Build();

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
