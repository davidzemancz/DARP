using DARP.Models;
using DARP.Services;
using DARP.Utils;
using DARP.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        private MainWindowModel _windowModel
        {
            get => (MainWindowModel)DataContext;
            set => DataContext = value;
        }
        private Random _random;

        private readonly IOrderDataService _orderService;
        private readonly IVehicleDataService _vehicleService;
        private readonly IPlanningService _planningService;
        private readonly ILoggerService _logger;

        public MainWindow()
        {
            InitializeComponent();
            _orderService = ServiceProvider.Default.GetService<IOrderDataService>();
            _vehicleService = ServiceProvider.Default.GetService<IVehicleDataService>();
            _planningService = ServiceProvider.Default.GetService<IPlanningService>();
            _logger = ServiceProvider.Default.GetService<ILoggerService>();
        }

        #region METHODS

        private void UpdatePlan()
        {
            foreach (VehicleView vehicleView in _vehicleService.GetVehicleViews())
            {
                if (!_planningService.Plan.Vehicles.Contains(vehicleView.GetModel()))
                {
                    _planningService.AddVehicle(_windowModel._currentTime, vehicleView.GetModel());
                }
            }

            IEnumerable<Order> newOrders = _orderService.GetOrderViews().Where(ov => ov.State == OrderState.Created).Select(ov => ov.GetModel());
            _planningService.UpdatePlan(_windowModel._currentTime, newOrders);

            dgOrders.Items.Refresh();
        }

        private void RenderPlan()
        {
            foreach (Route route in _planningService.Plan.Routes)
            {
                DataGrid dg = planRoutesStack.Children.OfType<DataGrid>().FirstOrDefault(dg => dg.Tag == route);
                if (dg == null)
                {
                    dg = new DataGrid() { Tag = route };
                    planRoutesStack.Children.Add(dg);
                }
                dg.ItemsSource = route.Points.Select(p => new RoutePointView(p));
            }

        }

        #endregion

        #region EVENT METHODS

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgOrders.ItemsSource = _orderService.GetOrderViews();
            dgVehicles.ItemsSource = _vehicleService.GetVehicleViews();

            _windowModel.MaxCords = (new Cords(20, 20)).ToString();
            _windowModel.Seed = ((int)DateTime.Now.Ticks).ToString();
            _windowModel.MaxDeliveryTimeMins = 60.ToString();
            _windowModel.MinTimeWindowMins = 10.ToString();
            _windowModel.MaxTimeWindowMins = 10.ToString();
            _windowModel.NewOrdersCount = 1.ToString();
            _windowModel.ReplanIntervalMins = 5.ToString();
            _windowModel.NewOrderIntervalMins = 5.ToString();
            _windowModel.VehicleSpeed = 1.ToString();

            _random = new Random(_windowModel._seed);

            _logger.TextWriters.Add(new TextBoxWriter(txtLog));

            _planningService.Init(XMath.ManhattanMetric);
        }

        private void newRandomOrder_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _windowModel._newOrdersCount; i++)
            {
                Time deliveryTwFrom = new Time(_windowModel.CurrentTime + _random.Next(_windowModel._maxDeliveryTimeMins));
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

        private void btnTick_Click(object sender, RoutedEventArgs e)
        {
            _windowModel.CurrentTime += 1;
        }

        private void btnUpdatePlan_Click(object sender, RoutedEventArgs e)
        {
            UpdatePlan();
            RenderPlan();
        }

        private void miSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new();
            sfd.DefaultExt = "json";
            sfd.Filter = "JSON Files | *.json";
            sfd.FileName = $"darp_{DateTime.Now.ToString("yyyyMMddHHmmss")}.json";
            if (sfd.ShowDialog() == true)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.Write(JsonSerializer.Serialize(
                        new MainWindowDataModel(
                            _orderService.GetOrderViews().Select(ov => ov.GetModel()),
                            _vehicleService.GetVehicleViews().Select(vv => vv.GetModel()),
                            _windowModel,
                            null
                            )));
                }
            }
        }

        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            ofd.DefaultExt = "json";
            ofd.Filter = "JSON Files | *.json";

            if (ofd.ShowDialog() == true)
            {
                using (StreamReader sr = new StreamReader(ofd.FileName))
                {
                    MainWindowDataModel dataModel = JsonSerializer.Deserialize<MainWindowDataModel>(sr.BaseStream);

                    _orderService.Clear();
                    _vehicleService.Clear();

                    foreach (var order in dataModel.Orders)
                    {
                        order.State = OrderState.Created;
                        _orderService.AddOrder(order);
                    }
                    foreach (var vehicle in dataModel.Vehicles)
                    {
                        _vehicleService.AddVehicle(vehicle);
                    }

                    //_planningService.Init(dataModel.Plan);
                    _windowModel = dataModel.WindowModel;

                    RenderPlan();
                }
            }
        }

        #endregion
    }

    internal class MainWindowDataModel
    {
        public IEnumerable<Order> Orders { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; }
        public MainWindowModel WindowModel { get; set; }
        public Plan Plan { get; set; }

        public MainWindowDataModel()
        {

        }

        public MainWindowDataModel(IEnumerable<Order> orders, IEnumerable<Vehicle> vehicles, MainWindowModel windowModel, Plan plan)
        {
            Orders = orders;
            Vehicles = vehicles;
            WindowModel = windowModel;
            Plan = plan;
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
        public Time _currentTime;

        public int CurrentTime
        {
            get => _currentTime.ToInt32();
            set
            {
                _currentTime = new Time(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
            }
        }
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
