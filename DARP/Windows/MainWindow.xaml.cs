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
using System.Windows.Automation;
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
        internal int CurrentTime { get; set; }

        private MainWindowModel _windowModel => (MainWindowModel) DataContext;
        private Random _random;
        
        private readonly IOrderDataService _orderService;
        private readonly IVehicleDataService _vehicleService;
        private readonly PlanningService _planningService;

        public MainWindow()
        {
            InitializeComponent();
            _orderService = ServiceProvider.Default.GetService<IOrderDataService>();
            _vehicleService = ServiceProvider.Default.GetService<IVehicleDataService>();
            _planningService = ServiceProvider.Default.GetService<PlanningService>();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgOrders.ItemsSource = _orderService.GetOrderViews();
            dgVehicles.ItemsSource = _vehicleService.GetVehicleViews();
            
            _windowModel.MaxCords = (new Cords(100,100)).ToString();
            _windowModel.Seed = ((int)DateTime.Now.Ticks).ToString();
            _windowModel.MaxDeliveryTimeMins = 60.ToString();
            _windowModel.MinTimeWindowMins= 5.ToString();
            _windowModel.MaxTimeWindowMins = 20.ToString();
            _windowModel.NewOrdersCount = 5.ToString();
            _windowModel.ReplanIntervalMins = 5.ToString();
            _windowModel.NewOrderIntervalMins = 5.ToString();
            _windowModel.VehicleSpeed = 5.ToString();

            _random = new Random(_windowModel._seed);
        }

     
        private void newRandomOrder_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _windowModel._newOrdersCount; i++)
            {
                Time deliveryTwFrom = new Time(CurrentTime + _random.Next(_windowModel._maxDeliveryTimeMins));
                _orderService.GetOrderViews().Add(new OrderView(new Order()
                {
                    PickupLocation = new Cords(_random.Next(0, (int)_windowModel._maxCords.X), _random.Next(0, (int)_windowModel._maxCords.Y)),
                    DeliveryLocation = new Cords(_random.Next(0, (int)_windowModel._maxCords.X), _random.Next(0, (int)_windowModel._maxCords.Y)),
                    DeliveryTimeWindow = new TimeWindow(deliveryTwFrom, new Time(deliveryTwFrom.Minutes + _random.Next(_windowModel._minTwMins, _windowModel._maxTwMins)))
                }));
            }
        }

        private void btnResetRnd_Click(object sender, RoutedEventArgs e)
        {
            _random = new Random(_windowModel._seed);
        }

        private void btnRunSimulation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btwNewRandomVehicle_Click(object sender, RoutedEventArgs e)
        {
            _vehicleService.GetVehicleViews().Add(new VehicleView(new Vehicle()
            {
                Name = "Taxi car",
                Location = new Cords(_random.Next(0, (int)_windowModel._maxCords.X), _random.Next(0, (int)_windowModel._maxCords.Y)),
            }));
        }
    }

    internal class MainWindowModel : INotifyPropertyChanged
    {
        public Cords _maxCords;
        public int _seed;
        public int _maxDeliveryTimeMins;
        public int _minTwMins;
        public int _maxTwMins;
        public int _newOrdersCount;
        public int _replanIntervalMins;
        public int _newOrderIntervalMins;
        public int _metric;
        public int _insertionMethod;
        public int _vehicleSpeed;

        public string VehicleSpeed
        {
            get => _vehicleSpeed.ToString();
            set
            {
                _vehicleSpeed = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VehicleSpeed)));
            }
        }
        public string InsertionMethod
        {
            get => _insertionMethod.ToString();
            set
            {
                _insertionMethod = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InsertionMethod)));
            }
        }
        public string Metric
        {
            get => _metric.ToString();
            set
            {
                _metric = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Metric)));
            }
        }
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

        public string Seed
        {
            get => _seed.ToString();
            set
            {
                _seed = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seed)));
            }
        }

        public string MaxDeliveryTimeMins
        {
            get => _maxDeliveryTimeMins.ToString();
            set
            {
                _maxDeliveryTimeMins = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxDeliveryTimeMins)));
            }
        }

        public string MinTimeWindowMins
        {
            get => _minTwMins.ToString();
            set
            {
                _minTwMins = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinTimeWindowMins)));
            }
        }

        public string MaxTimeWindowMins
        {
            get => _maxTwMins.ToString();
            set
            {
                _maxTwMins = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxTimeWindowMins)));
            }
        }

        public string NewOrdersCount
        {
            get => _newOrdersCount.ToString();
            set
            {
                _newOrdersCount = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewOrdersCount)));
            }
        }
        public string ReplanIntervalMins
        {
            get => _replanIntervalMins.ToString();
            set
            {
                _replanIntervalMins = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReplanIntervalMins)));
            }
        }
        public string NewOrderIntervalMins
        {
            get => _newOrderIntervalMins.ToString();
            set
            {
                _newOrderIntervalMins = int.Parse(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewOrderIntervalMins)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }




}
