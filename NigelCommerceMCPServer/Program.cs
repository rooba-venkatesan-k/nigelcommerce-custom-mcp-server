
using NigelCommerceMCPServer.Services;

namespace NigelCommerceMCPServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddStandardJwtAuthentication(builder.Configuration);
            builder.Services.AddControllers();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSingleton<McpSessionManager>();
            builder.Services.AddHttpClient<McpToolService>();

            builder.Services.AddAuthorization();

            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
