// Minimal ASP.NET Core setup for the SignalR server
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);

//// Configure Kestrel for HTTPS on port 50001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(50001); // HTTP
});

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(5000); // Bind to localhost only
//});

// Add SignalR services
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

// Add CORS to allow the WPF client to connect (update with your client's URL in production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors("AllowAll");

// Map the SignalR Hub
app.MapHub<LobbyHub>("/lobbyHub");

app.Run("http://0.0.0.0:50001");

//app.Run("http://0.0.0.0:5000");