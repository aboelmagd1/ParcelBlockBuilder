using System.Collections.Generic;
using System.Linq;

namespace ParcelBuilder.Core.Models
{
    public class ValidationMessage
    {
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Info;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TargetProperty { get; set; } = string.Empty;
        public ParcelSide? Side { get; set; }
        public int? ParcelSequence { get; set; }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    /// <summary>
    /// Consolidated validation result capturing errors, warnings, and informational notices.
    /// </summary>
    public class ValidationResult
    {
        public List<ValidationMessage> Messages { get; set; } = new List<ValidationMessage>();

        public bool IsValid => !Messages.Any(m => m.Severity == ValidationSeverity.Error);
        public bool HasWarnings => Messages.Any(m => m.Severity == ValidationSeverity.Warning);
        public bool HasErrors => Messages.Any(m => m.Severity == ValidationSeverity.Error);

        public void AddError(string code, string message, string targetProperty = "", ParcelSide? side = null, int? seq = null)
        {
            Messages.Add(new ValidationMessage
            {
                Severity = ValidationSeverity.Error,
                Code = code,
                Message = message,
                TargetProperty = targetProperty,
                Side = side,
                ParcelSequence = seq
            });
        }

        public void AddWarning(string code, string message, string targetProperty = "", ParcelSide? side = null, int? seq = null)
        {
            Messages.Add(new ValidationMessage
            {
                Severity = ValidationSeverity.Warning,
                Code = code,
                Message = message,
                TargetProperty = targetProperty,
                Side = side,
                ParcelSequence = seq
            });
        }

        public void AddInfo(string code, string message, string targetProperty = "")
        {
            Messages.Add(new ValidationMessage
            {
                Severity = ValidationSeverity.Info,
                Code = code,
                Message = message,
                TargetProperty = targetProperty
            });
        }
    }
}
