using System.Collections.Generic;
using System.Linq;

namespace UserService.Domain.Models
{
    public class ValidationResult
    {
        public bool IsSuccess => !Errors.Any();
        public List<string> Errors { get; private set; } = new List<string>();

        public void AddError(string error)
        {
            Errors.Add(error);
        }

        public static ValidationResult Success() => new ValidationResult();
        
        public static ValidationResult Fail(string error)
        {
            var result = new ValidationResult();
            result.AddError(error);
            return result;
        }
    }
}
