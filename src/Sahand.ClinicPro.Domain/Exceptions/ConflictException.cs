namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// تعارض داده (مثال: کد ملی تکراری)
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message, "CONFLICT") { }
}
