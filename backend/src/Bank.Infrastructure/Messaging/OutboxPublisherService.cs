using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank.Infrastructure.Messaging;

/// <summary>Runs <see cref="OutboxProcessor"/> in the background for as long as the API is up.</summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly OutboxProcessor _processor;
    private readonly MessagingOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        OutboxProcessor processor, IOptions<MessagingOptions> options, TimeProvider time, ILogger<OutboxPublisherService> logger)
    {
        _processor = processor;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = 0;
            try
            {
                published = await _processor.PublishPendingAsync(_options.BatchSize, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // A database or broker outage must not stop the loop; the rows are still there next time.
                _logger.LogError(e, "Outbox publishing failed");
            }

            // A full batch means there is probably more waiting; otherwise wait for new events.
            if (published < _options.BatchSize)
                await Task.Delay(_options.PollInterval, _time, stoppingToken);
        }
    }
}
