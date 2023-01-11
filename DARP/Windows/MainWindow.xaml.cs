using DARP.Models;
using DARP.Services;
using DARP.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        private readonly Settings _settings;

        private readonly IOrderDataService _orderService;
        private readonly IVehicleDataService _vehicleService;

        public MainWindow()
        {
            InitializeComponent();
            _orderService = ServiceProvider.Default.GetService<IOrderDataService>();
            _vehicleService = ServiceProvider.Default.GetService<IVehicleDataService>();

            _settings = new Settings();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgOrders.ItemsSource = _orderService.GetOrderViews();
            dgVehicles.ItemsSource = _vehicleService.GetVehicleViews();
            
            txtMaxCords.DataContext = _settings;
            
            _settings.MaxCords = (new Cords(100,100)).ToString();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void newRandomOrder_Click(object sender, RoutedEventArgs e)
        {
            _orderService.GetOrderViews().Add(new OrderView(new Order()
            {
                Name = "",
                PickupLocation = new Cords(6, 4),
                DeliveryLocation = new Cords(10, 1),
                DeliveryTimeWindow = new TimeWindow(new Time(5), new Time(8))
            }));
        }

        class Settings : INotifyPropertyChanged
        {
            private Cords _maxCords;

            public string MaxCords 
            { 
                get => _maxCords.ToString(); 
                set
                {
                    string[] arr = value.Split(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    _maxCords = new Cords(double.Parse(arr[0]), double.Parse(arr[1])); 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxCords)));
                } 
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }




}
