using FastGuide.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FastGuide.Tests;

public class ApiTests : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client;

    public ApiTests(ApiWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Channels_ReturnsOk()
    {
        var response = await _client.GetAsync("/channels");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Search_ReturnsOk()
    {
        var response = await _client.GetAsync("/search?query=News");
        response.EnsureSuccessStatusCode();
    }
}

public class ApiWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<FastGuideDbContext>));
            services.AddDbContext<FastGuideDbContext>(options => options.UseInMemoryDatabase("fastguide-tests"));

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FastGuideDbContext>();
            db.Database.EnsureCreated();
            db.Channels.Add(new FastGuide.Core.Models.Channel { Name = "News" });
            db.SaveChanges();
        });

        builder.UseSetting("StartupIngestionEnabled", "false");
    }
}
