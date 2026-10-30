using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>تشخیص محدود مسابقه درج هم‌زمان شناسه درخواست در SQL Server.</summary>
internal static class RequestIdConflict
{
    internal const string PaymentIndex = "IX_Payment_RequestId";
    internal const string InvoiceIndex = "IX_Invoice_RequestId";
    internal const string ReceptionIndex = "IX_Reception_RequestId";

    internal static bool IsRequestIdViolation(DbUpdateException exception, string index)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is SqlException sql && sql.Errors.Cast<SqlError>()
                .Any(error => IsRelevantSqlError(error.Number, error.Message, index)))
                return true;
        }
        return false;
    }

    // 2601/2627 به‌تنهایی کافی نیستند: سایر ایندکس‌های یکتا نباید به خطای
    // بازپخش نسبت داده شوند. نام دقیق ایندکس از پیام موتور SQL تطبیق می‌یابد.
    internal static bool IsRelevantSqlError(int number, string message, string index) =>
        (number is 2601 or 2627) && message.Contains($"'{index}'", StringComparison.OrdinalIgnoreCase);
}
