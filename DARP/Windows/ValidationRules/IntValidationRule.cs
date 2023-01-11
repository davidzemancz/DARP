using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DARP.Windows.ValidationRules
{
    public class IntValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string valueStr = (string)value ?? "";
            const string errMsg = "Invalid integer format.";
            return new ValidationResult(int.TryParse(valueStr, out _), errMsg);
        }
    }
}
