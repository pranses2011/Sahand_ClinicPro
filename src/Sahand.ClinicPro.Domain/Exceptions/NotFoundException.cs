namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// یافت نشدن موجودیت درخواستی
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($".موجودیت {entityName} با کلید {key} یافت نشد", "NOT_FOUND") { }
}
