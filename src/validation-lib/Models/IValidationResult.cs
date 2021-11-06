using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace validation_lib.Models
{
    public interface IValidationResult
    {
        bool WasSuccessful { get; }

        string FailureReason { get; }
    }
}
