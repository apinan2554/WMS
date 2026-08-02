namespace WH_Logistic.Services
{
    public interface IAuditService
    {
        Task LogAsync(int? userId, string action, string? module, string? details, string? locationCode = null);
    }
}
