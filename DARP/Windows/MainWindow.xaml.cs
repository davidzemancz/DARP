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
using System.Drawing;
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
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace DARP.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowModel WindowModel
        {
            get => (MainWindowModel)DataContext;
            set => DataContext = value;
        }

        private Random _random;
        private List<Timer> _timers;
        private Dictionary<(double, double), (double, double)> _cords;
        private PointCollection _vehicleShapePoints = new()
        {
            new Point(0, 3),
            new Point(0, 2),
            new Point(2, 2),
            new Point(2, 1),
            new Point(4, 1),
            new Point(4, 2),
            new Point(6, 2),
            new Point(6, 2),
            new Point(6, 3),
            new Point(6, 3),
            new Point(5, 3),
            new Point(5, 3.5),
            new Point(4, 3.5),
            new Point(4, 3),
            new Point(2, 3),
            new Point(2, 3.5),
            new Point(1, 3.5),
            new Point(1, 3),
        };
        private PointCollection _arrowUpShapePoints = new()
        {
            new Point(1, 3),
            new Point(1, 1),
            new Point(0, 1),
            new Point(1.5, 0),
            new Point(3, 1),
            new Point(2, 1),
            new Point(2, 3),
        };

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
            Time currentTime = Application.Current.Dispatcher.Invoke(() => WindowModel.CurrentTime);

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
            WindowModel.TotalDistance = _planningService.GetTotalDistance();

            planRoutesStack.Children.Clear();
            foreach (Route route in _planningService.Plan.Routes)
            {
                DataGrid dg = new() { Tag = route };
                planRoutesStack.Children.Add(dg);
                dg.ItemsSource = route.Points.Select(p => new RoutePointView(p));
            }

            var orderViews = _orderService.GetOrderViews();

            WindowModel.Stats.TotalOrders = orderViews.Count;
            WindowModel.Stats.HandledOrders = orderViews.Where(o => o.State == OrderState.Handled).Count();
            WindowModel.Stats.AcceptedOrders = orderViews.Where(o => o.State == OrderState.Handled || o.State == OrderState.Accepted).Count();
            WindowModel.Stats.RejectedOrders = orderViews.Where(o => o.State == OrderState.Rejected).Count();

            // TODO optimality index

        }

        private void AddRandomOrder()
        {
            Time deliveryTwFrom = new Time(WindowModel.CurrentTime.ToInt32() + WindowModel.Params.DeliveryTime.Min + _random.Next(WindowModel.Params.DeliveryTime.Max));
            _orderService.AddOrder(new Order()
            {
                PickupLocation = new Cords(_random.Next(0, WindowModel.Params.MapSize), _random.Next(0, (int)WindowModel.Params.MapSize)),
                DeliveryLocation = new Cords(_random.Next(0, WindowModel.Params.MapSize), _random.Next(0, (int)WindowModel.Params.MapSize)),
                DeliveryTimeWindow = new TimeWindow(deliveryTwFrom, new Time(deliveryTwFrom.Minutes + _random.Next(WindowModel.Params.DeliveryTimeWindow.Min, WindowModel.Params.DeliveryTimeWindow.Max))),
                Cost = WindowModel.Params.OrderCostPerDistanceUnit
            });
        }

        private void AddRandomOrders(int expectedCount)
        {
            int variance = WindowModel.Params.OrdersCountVariance;
            for (int i = 0; i < expectedCount * variance; i++)
            {
                if (Random.Shared.NextDouble() > (1.0 / variance)) continue;
                AddRandomOrder();
            }
        }


        private void DrawManhattanMap()
        {

            Color BG_COLOR = Colors.WhiteSmoke;
            double blockHeight = (cMap.ActualHeight / WindowModel.Params.MapSize);
            double blockWidth = (cMap.ActualWidth / WindowModel.Params.MapSize);

            cMap.Children.Clear();
            cMap.Background = new SolidColorBrush(BG_COLOR);

            // Horizontal roads
            Dictionary<int, double> cordsY = new();
            int yIndex = 0;
            for (double y = 0; y < cMap.ActualHeight; y += blockHeight)
            {
                cordsY[yIndex++] = y;
                DrawRoad(0, cMap.ActualWidth, y, y);
            }

            // Vertical roads
            Dictionary<int, double> cordsX = new();
            int xIndex = 0;
            for (double x = 0; x < cMap.ActualWidth; x += blockWidth)
            {
                cordsX[xIndex++] = x;
                DrawRoad(x, x, 0, cMap.ActualHeight);
            }

            // Coordinates mapping
            _cords = new();
            foreach ((int cordY, double y) in cordsY)
                foreach ((int cordX, double x) in cordsX)
                    _cords[(cordX, cordY)] = (x, y);

            // Routes
            if (chbDrawRoutes.IsChecked ?? false)
            {
                foreach (Route route in _planningService.Plan.Routes)
                {
                    DrawVehicle(route.Vehicle);
                    DrawRoute(route);
                }
            }

            // Orders
            if (chbDrawOrders.IsChecked ?? false)
            {
                foreach (OrderView orderView in _orderService.GetOrderViews())
                {
                    DrawOrder(orderView.GetModel());
                }
            }
        }

        private void DrawLegened()
        {
            // TODO draw legend

            //Polygon carShape = new()
            //{
            //    Points = _vehicleShapePoints,
            //    Fill = new SolidColorBrush(Colors.Black),
            //    Width = 16,
            //    Height = 16,
            //    Stretch = Stretch.Fill,
            //};
            //cLegend.Children.Add(carShape);

            //cLegend.Children.Add(new Label() { Content = "Car"});

            //Canvas.SetTop(carShape, 0);
            //Canvas.SetLeft(carShape, 0);
        }

        private void DrawRoute(Route route)
        {
            for (int i = 1; i < route.Points.Count; i++)
            {
                RoutePoint point1 = route.Points[i - 1];
                RoutePoint point2 = route.Points[i];
                (double p1X, double p1Y) = _cords[(point1.Location.X, point1.Location.Y)];
                (double p2X, double p2Y) = _cords[(point2.Location.X, point2.Location.Y)];

                DrawPath(p1X, p2X, p1Y, p2Y, route.Vehicle.Color);
            }
        }

        private void DrawVehicle(Vehicle vehicle)
        {
            const int VEHICLE_SIZE = 16;

            (double vehicleX, double vehicleY) = _cords[(vehicle.Location.X, vehicle.Location.Y)];

            Polygon vehicleShape = new()
            {
                Points = _vehicleShapePoints,
                Fill = new SolidColorBrush(vehicle.Color),
                Width = VEHICLE_SIZE,
                Height = VEHICLE_SIZE,
                Stretch = Stretch.Fill,
            };
            cMap.Children.Add(vehicleShape);

            Canvas.SetTop(vehicleShape, vehicleY - VEHICLE_SIZE / 2);
            Canvas.SetLeft(vehicleShape, vehicleX - VEHICLE_SIZE / 2);
        }

        private void DrawOrder(Order order)
        {
            const int ORDER_POINT_SIZE = 15;

            Color orderColor = GetRandomColor();

            // Pickup
            (double pickupX, double pickupY) = _cords[(order.PickupLocation.X, order.PickupLocation.Y)];
            Polygon pickupShape = new()
            {
                Points = _arrowUpShapePoints,
                Fill = new SolidColorBrush(orderColor),
                Width = ORDER_POINT_SIZE,
                Height = ORDER_POINT_SIZE,
                Stretch = Stretch.Fill,
            };
            cMap.Children.Add(pickupShape);
            Canvas.SetTop(pickupShape, pickupY - ORDER_POINT_SIZE / 2);
            Canvas.SetLeft(pickupShape, pickupX - ORDER_POINT_SIZE / 2);

            // Delivery
            (double deliveryX, double deliveryY) = _cords[(order.DeliveryLocation.X, order.DeliveryLocation.Y)];
            Polygon deliveryShape = new()
            {
                Points = _arrowUpShapePoints,
                Fill = new SolidColorBrush(orderColor),
                Width = ORDER_POINT_SIZE,
                Height = ORDER_POINT_SIZE,
                Stretch = Stretch.Fill,
                RenderTransform = new ScaleTransform(1,-1),
                RenderTransformOrigin = new Point(0.5,0.5)
            };
            cMap.Children.Add(deliveryShape);
            Canvas.SetTop(deliveryShape, deliveryY - ORDER_POINT_SIZE / 2);
            Canvas.SetLeft(deliveryShape, deliveryX - ORDER_POINT_SIZE / 2);

            // Route from pickup to delivery
            //DrawPath(pickupX, deliveryX, pickupY, deliveryY, orderColor);
        }

        private void DrawPath(double x1, double x2, double y1, double y2, Color color)
        {
            DrawLine(x1, x2, y1, y1, color, 3);
            DrawLine(x2, x2, y1, y2, color, 3);
        }

        private void DrawRoad(double x1, double x2, double y1, double y2)
        {
            DrawLine(x1, x2, y1, y2, Colors.LightGray, 5);
        }

        private void DrawLine(double x1, double x2, double y1, double y2, Color color, int thickness)
        {
            Line line = new()
            {
                X1 = x1,
                X2 = x2,
                Y1 = y1,
                Y2 = y2,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness
            };
            cMap.Children.Add(line);
        }

        private Color GetRandomColor()
        {
            return Color.FromRgb((byte)Random.Shared.Next(1, 255), (byte)Random.Shared.Next(1, 255), (byte)Random.Shared.Next(1, 233));
        }

        #endregion

        #region EVENT METHODS

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgOrders.ItemsSource = _orderService.GetOrderViews();
            dgVehicles.ItemsSource = _vehicleService.GetVehicleViews();

            pgSettings.ExpandAllProperties();

            _random = new Random(WindowModel.Params.Seed);
            _logger.TextWriters.Add(new TextBoxWriter(txtLog));
            _planningService.Init(new Plan(XMath.ManhattanMetric));

            _planningService.MIPSolverService.ParamsProvider.RetrieveMultithreading = () => Application.Current.Dispatcher.Invoke(() => WindowModel.Params.MIPMultithreading);
            _planningService.MIPSolverService.ParamsProvider.RetrieveTimeLimitSeconds = () => Application.Current.Dispatcher.Invoke(() => WindowModel.Params.MIPTimeLimit);
            _planningService.MIPSolverService.ParamsProvider.RetrieveObjective = () => Application.Current.Dispatcher.Invoke((() => WindowModel.Params.ObjectiveFunction));
            _planningService.MIPSolverService.ParamsProvider.RetrieveVehicleCharge = () => Application.Current.Dispatcher.Invoke(() => WindowModel.Params.VehicleCharge);
            _planningService.InsertionHeuristicsParamsProvider.RetrieveMode = () => Application.Current.Dispatcher.Invoke(() => WindowModel.Params.InsertionMode);

            DrawLegened();
        }

        private void newRandomOrder_Click(object sender, RoutedEventArgs e)
        {
            AddRandomOrder();
        }

        private void btnRunSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (!WindowModel.SimulationRunning)
            {
                if (_vehicleService.GetVehicleViews().Count == 0)
                {
                    MessageBox.Show("Add at least one vehicle", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                WindowModel.SimulationRunning = true;
                btnRunSimulation.Content = "Stop simulation";

                _timers = new() {
                    // Time
                    new Timer((state) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            WindowModel.CurrentTime = new Time(WindowModel.CurrentTime.Minutes + 1);
                        });
                    },
                    null, 0, 1000),
                    // New orders
                    new Timer((state) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            AddRandomOrders(WindowModel.Params.ExpectedOrdersCount);
                        });
                    }, null, 0, WindowModel.Params.GenerateNewOrderMins * 1000),
                    // Plan update
                    new Timer((state) =>
                    {
                        Task task = new(() => UpdatePlan());
                        task.Start();
                        task.ContinueWith(_ =>
                        {
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                RenderPlan();
                                lbTasks.Items.Refresh();
                            });
                        });
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            WindowModel.Tasks.Add(new TaskItem("Planning", task));
                            lbTasks.Items.Refresh();
                        });

                    }, null, WindowModel.Params.UpdatePlanMins * 1000, WindowModel.Params.UpdatePlanMins * 1000),
                };
            }
            else
            {
                WindowModel.SimulationRunning = false;
                btnRunSimulation.Content = "Run simulation";
                _timers.ForEach(timer => timer.Dispose());
                _timers.Clear();
            }

        }

        private void btwNewRandomVehicle_Click(object sender, RoutedEventArgs e)
        {
            _vehicleService.GetVehicleViews().Add(new VehicleView(new Vehicle()
            {
                Location = new Cords(_random.Next(0, WindowModel.Params.MapSize), _random.Next(0, WindowModel.Params.MapSize)),
                Color = GetRandomColor(),
            }));
        }

        private void btnTick_Click(object sender, RoutedEventArgs e)
        {
            WindowModel.CurrentTime = new Time(WindowModel.CurrentTime.Minutes + 1);
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
                            WindowModel,
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
                    WindowModel = dataModel.WindowModel;

                    RenderPlan();

                    pgSettings.ExpandAllProperties();
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


        private void btnRefreshMap_Click(object sender, RoutedEventArgs e)
        {
            DrawManhattanMap();
        }
    }


    #endregion


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
        public MainWindowStats Stats { get; set; } = new();
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public MainWindowParams Params { get; set; } = new();
    }

    [AddINotifyPropertyChangedInterface]
    internal class MainWindowStats
    {
        public int TotalOrders { get; set; } = 1;
        public int HandledOrders { get; set; }
        public int AcceptedOrders { get; set; }
        public int RejectedOrders { get; set; }

        public string TotalOrdersStr { get => $"Total orders: {TotalOrders}"; }
        public string AcceptedOrdersStr { get => $"Accepted orders: {AcceptedOrders} ({100 * AcceptedOrders / TotalOrders}%)"; }
        public string HandledOrdersStr { get => $"Handled orders: {HandledOrders} ({100 * HandledOrders / TotalOrders}%)"; }
        public string RejectedOrdersStr { get => $"Rejected orders: {RejectedOrders} ({100 * RejectedOrders / TotalOrders}%)"; }

    }

    internal class MainWindowParams
    {
        // ------------ Order generation ------------------
        [Category("Order generation")]
        [DisplayName("Delivery time window size.")]
        [ExpandableObject]
        public PropertyRange<int> DeliveryTimeWindow { get; set; } = new(15, 15);

        [Category("Order generation")]
        [DisplayName("Delivery time")]
        [Description("Delivery time since current time.")]
        [ExpandableObject]
        public PropertyRange<int> DeliveryTime { get; set; } = new(30, 60);

        [Category("Order generation")]
        [DisplayName("Cost")]
        [Description("Cost per distance unit.")]
        public int OrderCostPerDistanceUnit { get; set; } = 3;

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
        [Description("[ExpectedOrdersCount] * [OrdersCountVariance] orders is generated independently with probability 1 / [OrdersCountVariance].")]
        public int ExpectedOrdersCount { get; set; } = 1;

        [Category("Simulation")]
        [DisplayName("Orders count variance")]
        [Description("[ExpectedOrdersCount] * [OrdersCountVariance] orders is generated independently with probability 1 / [OrdersCountVariance].")]
        public int OrdersCountVariance { get; set; } = 5;

        // ------------ Map ------------------
        [Category("Map")]
        [DisplayName("Size")]
        [Description("Maps height and width")]
        public int MapSize { get; set; } = 20;

        [Category("Map")]
        [DisplayName("Metric")]
        public Metric Metric { get; set; } = Metric.Manhattan;

        // ------------ Vehicle ------------------
        [Category("Vehicles")]
        [DisplayName("Speed")]
        [Description("Distance traveled by each vehicle in one tick.")]
        public int Speed { get; set; } = 1;

        [Category("Vehicles")]
        [DisplayName("Charge")]
        [Description("Charge per distance unit of vehicle's route.")]
        public int VehicleCharge { get; set; } = 1;

        // ------------ Optimization ------------------
        [Category("Optimization")]
        [DisplayName("Method")]
        public OptimizationMethod OptimizationMethod { get; set; } = OptimizationMethod.MIP;

        [Category("Optimization")]
        [DisplayName("Insertion heuristics")]
        [Description("Insertion heuristics mode. A First fit inserts a order into first route found. A Best fit finds the most tight space where the order fits. Best fit might be slightly slower than First fit.")]
        public InsertionHeuristicsMode InsertionMode { get; set; } = InsertionHeuristicsMode.Disabled;

        [Category("Optimization")]
        [DisplayName("Objective function")]
        public ObjectiveFunction ObjectiveFunction { get; set; } = ObjectiveFunction.MinimizeDistance;

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

    internal class TaskItem
    {
        public string Name { get; set; }
        public Task Task { get; set; }
        public DateTime Created { get; }

        public TaskItem(string name, Task task)
        {
            Name = name;
            Task = task;
            Created = DateTime.Now;
        }

        public override string ToString()
        {
            return $"Task {Name}, status {Task.Status}, created at {Created:T}";
        }
    }


}
