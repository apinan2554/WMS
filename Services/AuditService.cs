using WH_Logistic.Data;
using WH_Logistic.Models;

namespace WH_Logistic.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;

        public AuditService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(int? userId, string action, string? module, string? details, string? locationCode = null)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                Module = module,
                Details = details,
                LocationCode = locationCode,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
