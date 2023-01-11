using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DARP.Windows.ValidationRules
{
    public class CordsValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string valueStr = (string)value ?? "";
            string[] arr = valueStr.Split(CultureInfo.CurrentCulture.TextInfo.ListSeparator);

            bool valid = arr.Length > 1 && double.TryParse(arr[0], out double val1) && val1 > 0 && double.TryParse(arr[1], out double val2) && val2 > 0;
            const string errMsg = "Invalid coordinates format.";

            return new ValidationResult(valid, errMsg);
        }
    }
}
