using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DARP.Utils
{
    public class TextBoxWriter : TextWriter
    {
        private TextBox _txtBox;
        public override Encoding Encoding => Encoding.Unicode;

        public TextBoxWriter(TextBox txtBox)
        {
            _txtBox = txtBox;
        }

        public override void WriteLine(string value)
        {
            _txtBox.Text += $"{value}{Environment.NewLine}";
        }
    }
}
