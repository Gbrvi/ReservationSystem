﻿using Reservation.Data;

namespace Reservation.Services
{
    public class BanCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BanCleanupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var now = DateTime.Now;

                    var usersToUnban = dbContext.Users
                       .Where(u => !string.IsNullOrEmpty(u.BanReason) && u.BannedUntil.HasValue && u.BannedUntil.Value <= now)
                       .ToList();

                    foreach (var user in usersToUnban)
                    {
                        user.BanReason = null;
                        user.BannedUntil = null;
                    }

                    if (usersToUnban.Any())
                    {
                        await dbContext.SaveChangesAsync();
                    }
                }

                // Intervalo entre verificações (ex.: a cada 1 hora)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

}
