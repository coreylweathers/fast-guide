using FastGuide.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FastGuide.Infrastructure.Data;

public class FastGuideDbContext(DbContextOptions<FastGuideDbContext> options) : DbContext(options)
{
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ProviderChannel> ProviderChannels => Set<ProviderChannel>();
    public DbSet<ProgramSlot> ProgramSlots => Set<ProgramSlot>();
    public DbSet<ProviderProgramSlot> ProviderProgramSlots => Set<ProviderProgramSlot>();
    public DbSet<IngestionLog> IngestionLogs => Set<IngestionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Channel>().HasIndex(c => c.Name);

        modelBuilder.Entity<ProviderChannel>()
            .HasIndex(pc => new { pc.ProviderName, pc.ProviderChannelId })
            .IsUnique();

        modelBuilder.Entity<ProgramSlot>()
            .HasIndex(ps => new { ps.ChannelId, ps.StartTimeUtc, ps.EndTimeUtc });

        modelBuilder.Entity<ProviderProgramSlot>()
            .HasIndex(ps => new { ps.ProviderName, ps.ProviderChannelId, ps.StartTimeUtc, ps.EndTimeUtc, ps.Title });
    }
}
