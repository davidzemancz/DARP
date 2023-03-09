using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DARP.Windows
{
    /// <summary>
    /// Interaction logic for TextBoxWindow.xaml
    /// </summary>
    internal partial class TextBoxWindow : Window
    {
        public TextBoxWindow()
        {
            InitializeComponent();
        }

        public bool ShowDialog(string title, string description, ref string value)
        {
            Title = title;
            lbDesc.Content = description;
            txtVal.Text = value;
            bool res = ShowDialog() ?? false;
            value = txtVal.Text;
            return res;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtVal.Focus();
            txtVal.SelectAll();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnOk_Click(sender, null);
            else if (e.Key == Key.Escape) btnCancel_Click(sender, null);
        }

      
    }
}
