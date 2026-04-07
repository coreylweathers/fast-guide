using FastGuide.Core.Interfaces;
using FastGuide.Core.Models;
using FastGuide.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FastGuide.Infrastructure.Ingestion;

public sealed class IngestionOrchestrator(
    IEnumerable<IProviderIngestionService> providers,
    IChannelNormalizer channelNormalizer,
    FastGuideDbContext dbContext,
    ILogger<IngestionOrchestrator> logger)
{
    public async Task RunIngestionAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            try
            {
                var payload = await provider.FetchAsync(cancellationToken);
                await PersistProviderPayloadAsync(payload, cancellationToken);
                await LogAsync(provider.ProviderName, "Success", "Ingestion completed successfully.", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Provider ingestion failed for {Provider}", provider.ProviderName);
                await LogAsync(provider.ProviderName, "Failure", ex.Message, cancellationToken);
            }
        }
    }

    private async Task PersistProviderPayloadAsync(NormalizedProviderData payload, CancellationToken cancellationToken)
    {
        var channels = await dbContext.Channels
            .Include(c => c.ProviderChannels)
            .ToListAsync(cancellationToken);

        var providerChannelMap = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);

        foreach (var incoming in payload.Channels)
        {
            var resolved = channelNormalizer.ResolveOrCreate(channels, incoming, payload.ProviderName);
            if (!channels.Contains(resolved))
            {
                channels.Add(resolved);
                dbContext.Channels.Add(resolved);
            }

            var existingProviderChannel = resolved.ProviderChannels.FirstOrDefault(pc =>
                pc.ProviderName.Equals(payload.ProviderName, StringComparison.OrdinalIgnoreCase)
                && pc.ProviderChannelId.Equals(incoming.ProviderChannelId, StringComparison.OrdinalIgnoreCase));

            if (existingProviderChannel is null)
            {
                existingProviderChannel = new ProviderChannel
                {
                    ProviderName = payload.ProviderName,
                    ProviderChannelId = incoming.ProviderChannelId,
                    ChannelName = incoming.ChannelName,
                    RawMetadata = incoming.RawMetadata,
                    Channel = resolved
                };
                resolved.ProviderChannels.Add(existingProviderChannel);
            }
            else
            {
                existingProviderChannel.ChannelName = incoming.ChannelName;
                existingProviderChannel.RawMetadata = incoming.RawMetadata;
            }

            providerChannelMap[incoming.ProviderChannelId] = resolved;
        }

        var horizonStart = DateTime.UtcNow.AddHours(-2);
        var horizonEnd = DateTime.UtcNow.AddHours(8);

        var existingProgramSlots = await dbContext.ProgramSlots
            .Where(ps => ps.EndTimeUtc > horizonStart && ps.StartTimeUtc < horizonEnd)
            .ToListAsync(cancellationToken);

        foreach (var providerProgram in payload.Programs)
        {
            if (!providerChannelMap.TryGetValue(providerProgram.ProviderChannelId, out var channel))
            {
                continue;
            }

            var existingProgram = existingProgramSlots.FirstOrDefault(ps =>
                ps.ChannelId == channel.Id
                && ps.StartTimeUtc == providerProgram.StartTimeUtc
                && ps.EndTimeUtc == providerProgram.EndTimeUtc
                && ps.Title == providerProgram.Title);

            existingProgram ??= dbContext.ProgramSlots.Add(new ProgramSlot
            {
                Channel = channel,
                Title = providerProgram.Title,
                Description = providerProgram.Description,
                StartTimeUtc = providerProgram.StartTimeUtc,
                EndTimeUtc = providerProgram.EndTimeUtc
            }).Entity;

            dbContext.ProviderProgramSlots.Add(new ProviderProgramSlot
            {
                ProviderName = payload.ProviderName,
                ProviderChannelId = providerProgram.ProviderChannelId,
                Title = providerProgram.Title,
                Description = providerProgram.Description,
                StartTimeUtc = providerProgram.StartTimeUtc,
                EndTimeUtc = providerProgram.EndTimeUtc,
                RawMetadata = providerProgram.RawMetadata,
                ProgramSlot = existingProgram
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LogAsync(string providerName, string status, string message, CancellationToken cancellationToken)
    {
        dbContext.IngestionLogs.Add(new IngestionLog
        {
            ProviderName = providerName,
            TimestampUtc = DateTime.UtcNow,
            Status = status,
            Message = message
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
