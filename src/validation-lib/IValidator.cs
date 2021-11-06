using validation_lib.Models;

namespace validation_lib
{
    public interface IValidator<T>
    {
        ValidationResult Validate(T item);
    }
}
