using MedicalRecordService.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalRecordService.Services;

public sealed class OutboxProcessorWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();
            List<Models.OutboxMessage> messages;
            lock (db.Gate)
            {
                messages = [.. db.OutboxMessages.Where(x => !x.Processed).OrderBy(x => x.CreatedAt).Take(20)];
            }

            foreach (var message in messages)
            {
                try
                {
                    logger.LogInformation("Published outbox event {EventName}: {Payload}", message.EventName, message.PayloadJson);
                    lock (db.Gate)
                    {
                        message.Processed = true;
                        message.ProcessedAt = DateTimeOffset.UtcNow;
                        message.Error = null;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    lock (db.Gate)
                    {
                        message.RetryCount++;
                        message.Error = ex.Message;
                        db.SaveChanges();
                    }
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
