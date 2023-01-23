using DARP.Models;
using DARP.Providers;
using DARP.Services;
using DARP.Utils;
using DARP.Views;
using Microsoft.Win32;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
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
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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
        private List<Timer> _timers;

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
            Time currentTime = Application.Current.Dispatcher.Invoke(() => _windowModel.CurrentTime);

            foreach (VehicleView vehicleView in _vehicleService.GetVehicleViews())
            {
                if (!_planningService.Plan.Vehicles.Contains(vehicleView.GetModel()))
                {
                    _planningService.AddVehicle(currentTime, vehicleView.GetModel());
                }
            }

            IEnumerable<Order> newOrders = _orderService.GetOrderViews().Where(ov => ov.State == OrderState.Created).Select(ov => ov.GetModel());
            _planningService.UpdatePlan(currentTime, newOrders);

        }

       

        private void RenderPlan()
        {
            dgOrders.Items.Refresh();
            _windowModel.TotalDistance = _planningService.GetTotalDistance();

            planRoutesStack.Children.Clear();
            foreach (Route route in _planningService.Plan.Routes)
            {
                DataGrid dg = new() { Tag = route };
                planRoutesStack.Children.Add(dg);
                dg.ItemsSource = route.Points.Select(p => new RoutePointView(p));
            }

        }

        private void AddRandomOrder()
        {
            Time deliveryTwFrom = new Time(_windowModel.CurrentTime.ToInt32() + _windowModel.Params.DeliveryTime.Min + _random.Next(_windowModel.Params.DeliveryTime.Max));
            _orderService.AddOrder(new Order()
            {
                PickupLocation = new Cords(_random.Next(0, _windowModel.Params.MapSize), _random.Next(0, (int)_windowModel.Params.MapSize)),
                DeliveryLocation = new Cords(_random.Next(0, _windowModel.Params.MapSize), _random.Next(0, (int)_windowModel.Params.MapSize)),
                DeliveryTimeWindow = new TimeWindow(deliveryTwFrom, new Time(deliveryTwFrom.Minutes + _random.Next(_windowModel.Params.DeliveryTimeWindow.Min, _windowModel.Params.DeliveryTimeWindow.Max)))
            });
        }

        private void AddRandomOrders(int expectedCount)
        {
            int variance = _windowModel.Params.OrdersCountVariance;
            for (int i = 0; i < expectedCount * variance; i++)
            {
                if (Random.Shared.NextDouble() > (1.0 / variance)) continue;
                AddRandomOrder();
            }
        }

        #endregion

        #region EVENT METHODS

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgOrders.ItemsSource = _orderService.GetOrderViews();
            dgVehicles.ItemsSource = _vehicleService.GetVehicleViews();

            pgSettings.ExpandAllProperties();

            _random = new Random(_windowModel.Params.Seed);
            _logger.TextWriters.Add(new TextBoxWriter(txtLog));
            _planningService.Init(new Plan(XMath.ManhattanMetric)); 

            _planningService.MIPSolverService.ParamsProvider.RetrieveMultithreading = () => _windowModel.Params.MIPMultithreading;
            _planningService.MIPSolverService.ParamsProvider.RetrieveTimeLimitSeconds = () => _windowModel.Params.MIPTimeLimit;
            _planningService.InsertionHeuristicsParamsProvider.RetrieveInsertionHeuristicsMode = () => _windowModel.Params.InsertionMode;
        }

        private void newRandomOrder_Click(object sender, RoutedEventArgs e)
        {
            AddRandomOrder();
        }

        private void btnRunSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (!_windowModel.SimulationRunning)
            {
                if (_vehicleService.GetVehicleViews().Count == 0)
                {
                    MessageBox.Show("Add at least one vehicle", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _windowModel.SimulationRunning = true;
                btnRunSimulation.Content = "Stop simulation";

                _timers = new() {
                    // Time
                    new Timer((state) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _windowModel.CurrentTime = new Time(_windowModel.CurrentTime.Minutes + 1);
                        });
                    },
                    null, 0, 1000),
                    // New orders
                    new Timer((state) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AddRandomOrders(_windowModel.Params.ExpectedOrdersCount);
                        });
                    }, null, 0, _windowModel.Params.GenerateNewOrderMins * 1000),
                    // Plan update
                    new Timer((state) =>
                    {
                         Application.Current.Dispatcher.Invoke(() =>
                         {
                             UpdatePlan();
                             RenderPlan();
                        });
                    }, null, _windowModel.Params.UpdatePlanMins * 1000, _windowModel.Params.UpdatePlanMins * 1000),
                };
            }
            else
            {
                _windowModel.SimulationRunning = false;
                btnRunSimulation.Content = "Run simulation";
                _timers.ForEach(timer => timer.Dispose());
                _timers.Clear();
            }
           


        }

        private void btwNewRandomVehicle_Click(object sender, RoutedEventArgs e)
        {
            _vehicleService.GetVehicleViews().Add(new VehicleView(new Vehicle()
            {
                Location = new Cords(_random.Next(0, _windowModel.Params.MapSize), _random.Next(0, _windowModel.Params.MapSize)),
            }));
        }

        private void btnTick_Click(object sender, RoutedEventArgs e)
        {
            _windowModel.CurrentTime = new Time(_windowModel.CurrentTime.Minutes + 1);
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

        private void tbcLeft_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tbcLeft.SelectedItem == tbiLog)
            {
                txtLog.ScrollToEnd();
            }
        }

        private void txtLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtLog.ScrollToEnd();
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

    [AddINotifyPropertyChangedInterface]
    internal class MainWindowModel 
    {
        public Time CurrentTime { get; set; }
        public double TotalDistance { get; set; }
        public bool SimulationRunning { get; set; }

        public MainWindowParams Params { get; set; } = new();
    }

    internal class MainWindowParams
    {
        // ------------ Order generation ------------------
        [Category("Order generation")]
        [DisplayName("Delivery time window size")]
        [ExpandableObject]
        public PropertyRange<int> DeliveryTimeWindow { get; set; } = new(15, 15);

        [Category("Order generation")]
        [DisplayName("Delivery time")]
        [Description("Delivery time since current time")]
        [ExpandableObject]
        public PropertyRange<int> DeliveryTime { get; set; } = new(30, 60);

        // ------------ Randomization ------------------
        [Category("Randomization")]
        public int Seed { get; set; } = (int)DateTime.Now.Ticks;

        // ------------ Simulation ------------------
        [Category("Simulation")]
        [DisplayName("Update plan each [minutes]")]
        public int UpdatePlanMins { get; set; } = 10;

        [Category("Simulation")]
        [DisplayName("New orders each [minutes]")]
        public int GenerateNewOrderMins { get; set; } = 2;

        [Category("Simulation")]
        [DisplayName("Expected orders count")]
        [Description("[ExpectedOrdersCount] * [OrdersCountVariance] orders is generated independently with probability 1 / [OrdersCountVariance]")]
        public int ExpectedOrdersCount { get; set; } = 1;

        [Category("Simulation")]
        [DisplayName("Orders count variance")]
        [Description("[ExpectedOrdersCount] * [OrdersCountVariance] orders is generated independently with probability 1 / [OrdersCountVariance]")]
        public int OrdersCountVariance { get; set; } = 5;

        // ------------ Map ------------------
        [Category("Map")]
        [DisplayName("Size")]
        [Description("Maps height and width")]
        public int MapSize { get; set; } = 10;

        [Category("Map")]
        [DisplayName("Metric")]
        public Metric Metric { get; set; }

        // ------------ Vehicle ------------------
        [Category("Vehicle")]
        [DisplayName("Speed")]
        public int Speed { get; set; } = 1;

        // ------------ Optimization ------------------
        [Category("Optimization")]
        [DisplayName("Insertion heuristics")]
        [Description("Insertion heuristics mode. A First fit inserts a order into first route found. A Best fit finds the most tight space where the order fits. Best fit might be slightly slower than First fit.")]
        public InsertionHeuristicsMode InsertionMode { get; set; } = InsertionHeuristicsMode.FirstFit;

        [Category("Optimization")]
        [DisplayName("Objective function")]
        public ObjectiveFunction ObjectiveFunction { get; set; }

        // ------------ MIP solver ------------------
        [Category("MIP solver")]
        [DisplayName("Time limit [seconds]")]
        [Description("MIP solver time limit in seconds. If set to zero, then solving time is unlimited.")]
        public int MIPTimeLimit { get; set; } = 10;

        [Category("MIP solver")]
        [DisplayName("Multithreading")]
        [Description("Enable multithreading for MIP solver. Uses half of available threads.")]
        public bool MIPMultithreading { get; set; } = false;
       
    }
    
    internal class PropertyRange<T>
    {
        [DisplayName("Min")]
        [PropertyOrder(0)]
        [Description("Minimal value")]
        public T Min { get; set; }

        [DisplayName("Max")]
        [PropertyOrder(1)]
        [Description("Maximal value")]
        public T Max { get; set; }

        public PropertyRange(T min, T max)
        {
            Min = min;
            Max = max;
        }

        public override string ToString()
        {
            return $"[{Min}{CultureInfo.CurrentCulture.TextInfo.ListSeparator}{Max}]";
        }
    }



}
