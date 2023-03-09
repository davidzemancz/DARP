using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DARP.Utils
{
    internal class TextBoxWriter : TextWriter
    {
        private TextBox _txtBox;
        public override Encoding Encoding => Encoding.Unicode;

        public TextBoxWriter(TextBox txtBox)
        {
            _txtBox = txtBox;
        }

        public override void WriteLine(string value)
        {
            if (Thread.CurrentThread == Application.Current.Dispatcher.Thread)
            {
                _txtBox.Text += $"{value}{Environment.NewLine}";
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => _txtBox.Text += $"{value}{Environment.NewLine}");
            }
        }
    }
}
