using DARP.Services;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DARP.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IOrderDataService _orderService;
        private readonly IVehicleDataService _vehicleService;

        public MainWindow()
        {
            InitializeComponent();
            _orderService = ServiceProvider.Default.GetService<IOrderDataService>();
            _vehicleService = ServiceProvider.Default.GetService<IVehicleDataService>();
        }

     
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgOrders.ItemsSource = _orderService.GetOrderViews();
            dgVehicles.ItemsSource = _vehicleService.GetVehicleViews();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }
    }
}
