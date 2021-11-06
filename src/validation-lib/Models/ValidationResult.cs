using System;
using System.Collections.Generic;

namespace validation_lib.Models
{
    public class ValidationResult : IValidationResult
    {
        public static ValidationResult SuccessfulResult
        {
            get
            {
                return new ValidationResult { WasSuccessful = true };
            }
        }

        public static ValidationResult FailureResult(string failureReason)
        {
            return new ValidationResult
            {
                WasSuccessful = false,
                FailureReason = failureReason
            };
        }

        public bool WasSuccessful { get; set; }

        public string FailureReason
        {
            get
            {
                return string.Join(Environment.NewLine, _failureReasons ?? new List<string>());
            }
            set
            {
                _failureReasons = new List<string>();
                if (!string.IsNullOrEmpty(value))
                {
                    _failureReasons.Add(value);
                }
            }
        }

        private List<string> _failureReasons = new List<string>();

        public void AddFailureReason(string failureReason)
        {
            _failureReasons.Add(failureReason);
        }

        public void Combine(ValidationResult validationResult)
        {
            if (validationResult == null)
            {
                return;
            }

            if (validationResult.WasSuccessful)
            {
                return;
            }

            WasSuccessful = false;
            AddFailureReason(validationResult.FailureReason);
        }
    }
}
