using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using ClosedXML.Excel;
using DARP.Models;
using DARP.Providers;
using DARP.Services;
using DARP.Solvers;
using DARP.Utils;
using DARP.Views;
using Google.OrTools.Sat;
using MapControl;
using MapControl.Caching;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.Wpf;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using DataPoint = OxyPlot.DataPoint;
using Order = DARP.Models.Order;
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
        public MainWindow()
        {
            InitializeComponent();
            _orderService = ServiceProvider.Instance.GetService<IOrderDataService>();
            _vehicleService = ServiceProvider.Instance.GetService<IVehicleDataService>();
            _planDataService = ServiceProvider.Instance.GetService<IPlanDataService>();

            _mainWindowParams = new();
            _mainWindowModels = new();
            _mainWindowModels.CollectionChanged += (o, e) =>
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach(MainWindowModel newItem in e.NewItems)
                            _mainWindowParams.Add(newItem.Params);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        foreach (MainWindowModel newItem in e.NewItems)
                            _mainWindowParams.Remove(newItem.Params);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        throw new NotImplementedException();
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        throw new NotImplementedException();
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        throw new NotImplementedException();
                }
            };

            ImageLoader.HttpClient.DefaultRequestHeaders.Add("User-Agent", "XAML Map Control Test Application");
            TileImageLoader.Cache = new ImageFileCache(TileImageLoader.DefaultCacheFolder);

            map.Background = System.Windows.Media.Brushes.Red;
            map.MapLayer = new MapTileLayer()
            {
                TileSource = new TileSource() { UriTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png" } ,
                SourceName = "OpenStreetMap",
                Description = "© [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)"
            };

            if (TileImageLoader.Cache is ImageFileCache cache)
            {
                Loaded += async (s, e) =>
                {
                    await Task.Delay(2000);
                    await cache.Clean();
                };
            }
        }

        #region CONSTS

        private const string LAYOUT_FILE = @".\darp_layout.config";
        private const string MANUAL_CZ = @".\DARP manual CZ.pdf";

        #endregion

        #region PROPS

        private MainWindowModel WindowModel
        {
            get => (MainWindowModel)DataContext;
            set => DataContext = value;
        }

        #endregion

        #region FIELDS

        #region Shapes

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
        private PointCollection _elephantShapePoints = new()
        {
            new Point(3, 6),
            new Point(2, 6),
            new Point(2, 5),
            new Point(2, 4),
            new Point(1, 4),
            new Point(1, 3.5),
            new Point(0.5, 3.5),
            new Point(0.5, 5),
            new Point(0, 5),
            new Point(0, 4),
            new Point(0, 3),
            new Point(1, 3),
            new Point(1, 2),
            new Point(2, 1),
            new Point(3, 0),
            new Point(3, 2),
            new Point(5.5, 2),
            new Point(5.5, 2.5),
            new Point(5, 2.5),
            new Point(5, 6),
            new Point(4, 6),
            new Point(4, 4),
            new Point(3, 4),
        };
        private PointCollection _heartShapePoints = new()
        {
            new Point(3, 6),
            new Point(2, 5),
            new Point(1, 4),
            new Point(0, 3),
            new Point(0, 2),
            new Point(1, 1),
            new Point(2, 1),
            new Point(3, 2),
            new Point(4, 1),
            new Point(5, 1),
            new Point(6, 2),
            new Point(6, 3),
            new Point(5, 4),
            new Point(4, 5),
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

        #endregion

        private Random _random;
        private List<Timer> _timers;
        private Dictionary<(double, double), (double, double)> _cords;
        private LineSeries _handledOrdersSeries;
        private LineSeries _rejectedOrdersSeries;
        private LineSeries _totalProfitSeries;
        private LineSeries _profitOptimalitySeries;
        private LineSeries _travelTimeOptimalitySeries;
        private LineSeries _evolutionAvgFitnessSeries;
        private int _seriesLength;

        private readonly IOrderDataService _orderService;
        private readonly IVehicleDataService _vehicleService;
        private readonly IPlanDataService _planDataService;

        private readonly ObservableCollection<MainWindowModel> _mainWindowModels;
        private readonly ObservableCollection<MainWindowParams> _mainWindowParams;

        #endregion

        #region METHODS

        #region Vehicles

        private Vehicle GetRandomVehicle(MainWindowModel model)
        {
            var vehicle = new Vehicle()
            {
                Location = new Cords(_random.Next(0, model.Params.MapSize), _random.Next(0, model.Params.MapSize)),
            };
            return vehicle;
        }

        private void AddRandomVehicle()
        {
           
            Vehicle vehicle = GetRandomVehicle(WindowModel);
            _vehicleService.GetVehicleViews().Add(new VehicleView(vehicle) { Color = GetRandomColor(), ShowOnMap = true }); ;
            _planDataService.GetPlan().Routes.Add(new Route(vehicle, WindowModel.CurrentTime));

            LoggerBase.Instance.Debug($"Added vehicle {vehicle.Id}");
        }

        #endregion

        #region Orders

        private Order GetRandomOrder(MainWindowModel model)
        {
            Cords pickup = new Cords(_random.Next(0, model.Params.MapSize), _random.Next(0, (int)model.Params.MapSize));
            Cords delivery = new Cords(_random.Next(0, model.Params.MapSize), _random.Next(0, (int)model.Params.MapSize));

            double totalProfit = model.Params.OrderProfitPerTick * XMath.GetMetric(model.Params.Metric)(pickup, delivery).Ticks;

            Time maxDeliveryTimeFrom = new Time(model.CurrentTime.ToDouble() + model.Params.MaxDeliveryTimeFrom.Min + _random.Next(model.Params.MaxDeliveryTimeFrom.Max));
            Time maxDeliveryTimeTo = maxDeliveryTimeFrom + new Time(model.Params.OrderTimeWindowTicks);

            Order order = new()
            {
                PickupLocation = pickup,
                DeliveryLocation = delivery,
                DeliveryTime = new TimeWindow(maxDeliveryTimeFrom, maxDeliveryTimeTo),
                TotalProfit = totalProfit
            };

            return order;
        }

        private void AddRandomOrder()
        {
            Order order = GetRandomOrder(WindowModel);
            _orderService.AddOrder(order);
            LoggerBase.Instance.Debug($"Added order {order.Id}");
        }

        private void AddRandomOrders(int expectedCount)
        {
            int variance = WindowModel.Params.OrdersCountVariance;
            for (int i = 0; i < expectedCount * variance; i++)
            {
                if (_random.NextDouble() > (1.0 / variance)) continue;
                AddRandomOrder();
            }
        }

        private List<Order> GetOrdersToSchedule()
        {
            return _orderService.GetOrderViews().Select(ov => ov.GetModel()).Where(o => o.State == OrderState.Created || o.State == OrderState.Accepted).ToList();
        }

        private List<Order> GetOrdersToInsert()
        {
            return _orderService.GetOrderViews().Select(ov => ov.GetModel()).Where(o => o.State == OrderState.Created).ToList();
        }

        #endregion

        #region Time
        private void Tick()
        {
            if (WindowModel.CurrentTime.ToDouble() % WindowModel.Params.TimeSeriesUpdateEachTicks == 0) 
                UpdateTimeSeries();

            WindowModel.CurrentTime = new Time(WindowModel.CurrentTime.Ticks + 1);
            LoggerBase.Instance.Debug($"Tick {WindowModel.CurrentTime}");
        }

        #endregion

        #region Optimization

        private InsertionHeuristicsOutput RunInsertionHeuristics()
        {
            LoggerBase.Instance.Debug($"Run insertion heuristics");
            LoggerBase.Instance.StopwatchStart();

            InsertionHeuristics insertion = new();
            InsertionHeuristicsOutput output = insertion.Run(new InsertionHeuristicsInput()
            {
                Mode = WindowModel.Params.InsertionMode,
                Metric = XMath.GetMetric(WindowModel.Params.Metric),
                Orders = GetOrdersToInsert(),
                Vehicles = _vehicleService.GetVehicleViews().Select(vv => vv.GetModel()),
                Time = WindowModel.CurrentTime,
                Plan = _planDataService.GetPlan(),
                VehicleChargePerTick = WindowModel.Params.VehicleChargePerTick,
            });

            LoggerBase.Instance.StopwatchStop();

            _planDataService.SetPlan(output.Plan);
            return output;
        }

        private MIPSolverOutput RunMIPSolver()
        {
            LoggerBase.Instance.Debug($"Run MIP");
            LoggerBase.Instance.StopwatchStart();

            MIPSolver solver = new();
            MIPSolverOutput output = solver.Run(new MIPSolverInput()
            {
                TimeLimit = WindowModel.Params.MIPTimeLimit,
                Multithreading = WindowModel.Params.MIPMultithreading,
                Objective = WindowModel.Params.MIPObjective,
                Metric = XMath.GetMetric(WindowModel.Params.Metric),
                Orders = GetOrdersToSchedule(),
                Vehicles = _vehicleService.GetVehicleViews().Select(vv => vv.GetModel()),
                Time = WindowModel.CurrentTime,
                Plan = _planDataService.GetPlan(),
                VehicleChargePerTick = WindowModel.Params.VehicleChargePerTick,
            });

            LoggerBase.Instance.StopwatchStop();

            _planDataService.SetPlan(output.Plan);
            return output;
        }

        private async Task RunEvolution()
        {
            LoggerBase.Instance.Debug($"Run evolution");
            LoggerBase.Instance.StopwatchStart();

            int gens = WindowModel.Params.EvoGenerations;
            _evolutionAvgFitnessSeries.Points.Clear();

            EvolutionarySolver solver = new();
            EvolutionarySolverInput input = new EvolutionarySolverInput()
            {
                Generations = gens,
                PopulationSize = WindowModel.Params.EvoPopSize,
                Metric = XMath.GetMetric(WindowModel.Params.Metric),
                Orders = GetOrdersToSchedule(),
                Vehicles = _vehicleService.GetVehicleViews().Select(vv => vv.GetModel()),
                Time = WindowModel.CurrentTime,
                Plan = _planDataService.GetPlan(),
                VehicleChargePerTick = WindowModel.Params.VehicleChargePerTick,
                RandomOrderInsertMutProb = WindowModel.Params.RandomOrderInsertMutProb,
                RandomOrderRemoveMutProb = WindowModel.Params.RandomOrderRemoveMutProb,
                BestfitOrderInsertMutProb = WindowModel.Params.BestfitOrderInsertMutProb,
                RouteCrossoverProb = WindowModel.Params.RouteCrossoverProb,
                PlanCrossoverProb = WindowModel.Params.PlanCrossoverProb,
                EnviromentalSelection = WindowModel.Params.EnviromentalSelection,   
                FitnessLog = (gen, fittness) =>
                {
                    _evolutionAvgFitnessSeries.Points.Add(new DataPoint(gen, fittness[0]));

                    if (gen % (gens / 10) == 0)
                        Application.Current.Dispatcher.BeginInvoke(() => WindowModel.EvolutionPlot.InvalidatePlot(true));
                },
            };
            EvolutionarySolverOutput output = await Task.Run(() => solver.Run(input));
            _planDataService.SetPlan(output.Plan);
            LoggerBase.Instance.StopwatchStop();
        }

        private async Task RunPlan()
        {
            if (WindowModel.Params.UseInsertionHeuristics)
                RunInsertionHeuristics();

            switch (WindowModel.Params.OptimizationMethod)
            {
                case OptimizationMethod.Disabled:
                    break;
                case OptimizationMethod.MIP:
                    RunMIPSolver();
                    break;
                case OptimizationMethod.Evolutionary:
                    await RunEvolution();
                    break;
                case OptimizationMethod.AntColony:
                    throw new NotImplementedException();
            }

            // Reject old orders
            foreach (OrderView orderView in _orderService.GetOrderViews())
            {
                if (orderView.State != OrderState.Handled && orderView.DeliveryToTick < WindowModel.CurrentTime.Ticks)
                {
                    orderView.GetModel().Reject();
                }
            }
        }

        private void UpdatePlan()
        {
            LoggerBase.Instance.Debug($"Update plan");

            // Update plan
            (double profit, List<Order> removedOrders) = _planDataService.GetPlan().UpdateVehiclesLocation(WindowModel.CurrentTime, XMath.GetMetric(WindowModel.Params.Metric), WindowModel.Params.VehicleChargePerTick);
            removedOrders.ForEach(o => o.Handle());
            WindowModel.Stats.TotalProfit += profit;
        }

        private void RenderPlan()
        {
            dgOrders.Items.Refresh();

            planRoutesStack.Children.Clear();
            foreach (Route route in _planDataService.GetPlan().Routes)
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

            WindowModel.Stats.CurrentProfit = _planDataService.GetPlan().GetTotalProfit(XMath.GetMetric(WindowModel.Params.Metric), WindowModel.Params.VehicleChargePerTick);
        }

        #region Map

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
                foreach (Route route in _planDataService.GetPlan().Routes)
                {
                    VehicleView vv = _vehicleService.GetVehicleViews().First(vv => vv.GetModel() == route.Vehicle);
                    if (vv.ShowOnMap)
                    {
                        DrawVehicle(route.Vehicle);
                        DrawRoute(route);
                    }
                }
            }

            // Orders
            if (chbDrawOrders.IsChecked ?? false)
            {
                foreach (Order order in GetOrdersToSchedule())
                {
                    DrawOrder(order, GetRandomColor());
                }
            }
        }

        private void DrawMapLegened()
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

                VehicleView vv = _vehicleService.GetVehicleViews().First(vv => vv.GetModel() == route.Vehicle);
                DrawPath(p1X, p2X, p1Y, p2Y, vv.Color);

                Color orderColor = Color.Multiply(vv.Color, 2);
                if (point2 is OrderPickupRoutePoint oprp)
                {
                    DrawOrder(oprp.Order, orderColor);
                }
                else if (point2 is OrderDeliveryRoutePoint odrp)
                {
                    DrawOrder(odrp.Order, orderColor);
                }
            }
        }

        private void DrawVehicle(Vehicle vehicle)
        {
            const int VEHICLE_SIZE = 16;
            VehicleView vv = _vehicleService.GetVehicleViews().First(vv => vv.GetModel() == vehicle);

            (double vehicleX, double vehicleY) = _cords[(vehicle.Location.X, vehicle.Location.Y)];

            Polygon vehicleShape = new()
            {
                Points = _vehicleShapePoints,
                Fill = new SolidColorBrush(vv.Color),
                Width = VEHICLE_SIZE,
                Height = VEHICLE_SIZE,
                Stretch = Stretch.Fill,
            };
            cMap.Children.Add(vehicleShape);

            Canvas.SetTop(vehicleShape, vehicleY - VEHICLE_SIZE / 2);
            Canvas.SetLeft(vehicleShape, vehicleX - VEHICLE_SIZE / 2);
        }

        private void DrawOrder(Order order, Color orderColor)
        {
            const int ORDER_POINT_SIZE = 15;

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
                RenderTransform = new ScaleTransform(1, -1),
                RenderTransformOrigin = new Point(0.5, 0.5)
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

        #endregion

        #region Simulation templates

        private void SaveParamsAsSimulationTemplate()
        {
            _mainWindowModels.Add(WindowModel.Clone());
        }

        #endregion

        #region Simulation

        private async void RunSimulationTemplates()
        {
            List<Task> tasks = new();
            //for (int run = 0; run < WindowModel.Params.TemplateTotalRuns; run++)
            foreach(MainWindowModel model in _mainWindowModels)
            {
                var task = Task.Run(() => RunSimulationTemplate(model));
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            btnRunSimTemplates.IsChecked = false;
            WindowModel.SimulationTemplatesState = MainWindowModel.SimulationStateEnum.Ready;
        }

        private void RunSimulationTemplate(MainWindowModel model)
        {
            LoggerBase.Instance.DisplayThread = true;
            LoggerBase.Instance.Debug($"Started simulation template {model.Params.TemplateName}");
            LoggerBase.Instance.StopwatchStart();

            for (int run = 1; run <= model.Params.TemplateTotalRuns; run++)
            {
                LoggerBase.Instance.Debug($"Started run {run}");
                LoggerBase.Instance.StopwatchStart();

                model.CurrentTime = Time.Zero;
                Random random = new();
                Plan plan = new();
                MetricFunc metric = XMath.GetMetric(model.Params.Metric);
                double vehicleCharge = model.Params.VehicleChargePerTick;
                List<Order> orders = new();
                List<Vehicle> vehicles = new();
                vehicles.AddMany(() => GetRandomVehicle(model), model.Params.TemplateVehiclesCount);
                int vehicleId = 0;
                foreach (Vehicle vehicle in vehicles)
                {
                    vehicle.Id = ++vehicleId;
                    plan.Routes.Add(new Route(vehicle, model.CurrentTime));
                }

                Time end = new(model.Params.TemplateTotalTicks);
                double totalProfit = 0;
                int orderId = 0;

                for (; model.CurrentTime <= end; model.CurrentTime = model.CurrentTime.AddTicks(1))
                {
                    // New orders
                    if (model.CurrentTime.Ticks % model.Params.GenerateNewOrderTicks == 0)
                    {
                        List<Order> newOrders = new();
                        int variance = model.Params.OrdersCountVariance;
                        for (int i = 0; i < model.Params.ExpectedOrdersCount * variance; i++)
                        {
                            if (random.NextDouble() > (1.0 / variance)) continue;
                            Order order = GetRandomOrder(model);
                            order.Id = ++orderId;
                            newOrders.Add(order);
                        }
                        orders.AddRange(newOrders);
                    }

                    // Update plans
                    (double profit, List<Order> removedOrders) = plan.UpdateVehiclesLocation(model.CurrentTime, metric, vehicleCharge);
                    removedOrders.ForEach(o => o.Handle());
                    totalProfit += profit;

                    // Reject old orders
                    foreach (Order order in orders)
                        if (order.State != OrderState.Handled && order.DeliveryTime.To < model.CurrentTime)
                            order.Reject();

                    // Run optimization each n ticks
                    if (model.CurrentTime.Ticks % model.Params.UpdatePlanTicks == 0)
                    {
                        // Insertion heuristics
                        if (model.Params.UseInsertionHeuristics)
                        {
                            InsertionHeuristics insertion = new();
                            LoggerBase.Instance.Debug("Running insertion heuristics");
                            LoggerBase.Instance.StopwatchStart();
                            InsertionHeuristicsOutput output = insertion.Run(new InsertionHeuristicsInput()
                            {
                                Mode = model.Params.InsertionMode,
                                Metric = metric,
                                Orders = orders.Where(o => o.State == OrderState.Created),
                                Vehicles = vehicles,
                                Time = model.CurrentTime,
                                Plan = plan,
                                VehicleChargePerTick = vehicleCharge,
                            });
                            LoggerBase.Instance.StopwatchStop();
                            plan = output.Plan;
                        }

                        // Evolution
                        if (model.Params.OptimizationMethod == OptimizationMethod.Evolutionary)
                        {
                            EvolutionarySolver solver = new();
                            EvolutionarySolverInput input = new EvolutionarySolverInput()
                            {
                                Generations = model.Params.EvoGenerations,
                                PopulationSize = model.Params.EvoPopSize,
                                Metric = metric,
                                Orders = orders.Where(o => o.State == OrderState.Created || o.State == OrderState.Accepted),
                                Vehicles = vehicles,
                                Time = model.CurrentTime,
                                Plan = plan,
                                VehicleChargePerTick = model.Params.VehicleChargePerTick,
                                RandomOrderInsertMutProb = model.Params.RandomOrderInsertMutProb,
                                RandomOrderRemoveMutProb = model.Params.RandomOrderRemoveMutProb,
                                BestfitOrderInsertMutProb = model.Params.BestfitOrderInsertMutProb,
                                EnviromentalSelection = model.Params.EnviromentalSelection,
                                RouteCrossoverProb = model.Params.RouteCrossoverProb,
                                PlanCrossoverProb = model.Params.PlanCrossoverProb,
                            };
                            LoggerBase.Instance.Debug("Running evolutionary solver");
                            LoggerBase.Instance.StopwatchStart();
                            EvolutionarySolverOutput output = solver.Run(input);
                            LoggerBase.Instance.StopwatchStop();
                            plan = output.Plan;
                        }

                        LoggerBase.Instance.Debug($"Time {model.CurrentTime}, " +
                           $"Total profit {totalProfit + plan.GetTotalProfit(metric, vehicleCharge)}, " +
                           $"Total orders {orders.Count()}, " +
                           $"Handled orders {orders.Count(o => o.State == OrderState.Handled)}, " +
                           $"Rejected orders {orders.Count(o => o.State == OrderState.Rejected)}, " +
                           $"");
                    }
                }

                // Optimum estimation
                model.CurrentTime = Time.Zero;
                Plan estPlan = new();
                foreach (var vehicle in vehicles)
                {
                    estPlan.Routes.Add(new Route(vehicle, model.CurrentTime));
                }
                MIPSolverInput mipInput = new()
                {
                    //TimeLimit = 30_000,
                    Multithreading = false,
                    Objective = model.Params.MIPObjective,
                    Metric = metric,
                    Orders = orders,
                    Vehicles = vehicles,
                    Time = model.CurrentTime,
                    Plan = estPlan,
                    VehicleChargePerTick = vehicleCharge,
                    Integer = false, // Linear relaxation
                };
                MIPSolver ms = new();
                LoggerBase.Instance.Debug($"Running MIP solver, Time limit {mipInput.TimeLimit / 1000} seconds");
                LoggerBase.Instance.StopwatchStart();
                MIPSolverOutput mipOutput = ms.Run(mipInput);
                LoggerBase.Instance.StopwatchStop();
                LoggerBase.Instance.Debug($"Optimum estimation {mipOutput.ObjetiveValue}");

                LoggerBase.Instance.StopwatchStop();
                LoggerBase.Instance.Debug($"Finished run {run}");
            }

            LoggerBase.Instance.StopwatchStop();
            LoggerBase.Instance.Debug($"Finished simulation template {model.Params.TemplateName}");
            
        }

        private void StartSimulation()
        {
            int tickEachMillis = WindowModel.Params.TickEach * 1000;

            _timers = new() {
                    // Time
                    new Timer((state) =>
                    Application.Current.Dispatcher.BeginInvoke(() => Tick()),
                    null, 0, tickEachMillis),
                    // New orders
                    new Timer((state) =>
                    Application.Current.Dispatcher.BeginInvoke(() => AddRandomOrders(WindowModel.Params.ExpectedOrdersCount)),
                    null, 0, WindowModel.Params.GenerateNewOrderTicks * tickEachMillis),
                    // Plan update
                    new Timer((state) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(async () =>
                        {
                            await RunPlan();
                            UpdatePlan();
                            RenderPlan();
                        });

                    },
                    null, WindowModel.Params.UpdatePlanTicks * tickEachMillis, WindowModel.Params.UpdatePlanTicks * tickEachMillis),
                };
        }

        private void StopSimulation()
        {
            _timers.ForEach(timer => timer.Dispose());
            _timers.Clear();
        }

        #endregion

        #region Random

        private void ResetRandom()
        {
            _random = new Random(WindowModel.Params.Seed);
        }

        #endregion

        #region Time series

        private void ExportTimeSeriesCsv()
        {

            SaveFileDialog sfd = new();
            sfd.DefaultExt = "csv";
            sfd.Filter = "CSV Files | *.csv";
            sfd.FileName = $"darp_timeSeries_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv";
            if (sfd.ShowDialog() == true)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    string sep = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                    sw.WriteLine($"Tick{sep}Profit{sep}HandledOrder{sep}RejectedOrders{sep}ProfitOpt{sep}TravelTimeOpt{sep}");
                    for (int i = 0; i < _seriesLength; i++)
                    {
                        double tick = _totalProfitSeries.Points[i].X;

                        double profit = _totalProfitSeries.Points[i].Y;
                        double handledOrders = _handledOrdersSeries.Points[i].Y;
                        double rejectedOrders = _rejectedOrdersSeries.Points[i].Y;
                        double profitOpt = _profitOptimalitySeries.Points[i].Y;
                        double travelTimeOpt = _travelTimeOptimalitySeries.Points[i].Y;

                        StringBuilder sb = new();
                        sb.Append(tick);
                        sb.Append(sep);
                        sb.Append(profit);
                        sb.Append(sep);
                        sb.Append(handledOrders);
                        sb.Append(sep);
                        sb.Append(rejectedOrders);
                        sb.Append(sep);
                        sb.Append(profitOpt);
                        sb.Append(sep);
                        sb.Append(travelTimeOpt);
                        sb.Append(sep);
                        sw.WriteLine(sb);
                    }
                }
            }
        }

        private void UpdateTimeSeries()
        {
            _seriesLength++;

            // Profit
            _totalProfitSeries.Points.Add(new DataPoint(WindowModel.CurrentTime.ToDouble(), WindowModel.Stats.TotalProfit));

            // Optimality
            double optimalProfit = _orderService.GetOrderViews().Sum(ov => ov.Profit);
            double profitOptimality = 100 * (WindowModel.Stats.TotalProfit / optimalProfit);
            _profitOptimalitySeries.Points.Add(new DataPoint(WindowModel.CurrentTime.ToDouble(), profitOptimality));

            double totalTravelTime = _planDataService.GetPlan().Routes.Sum(r => r.Points[^1].Time.ToDouble());
            MetricFunc metric = XMath.GetMetric(WindowModel.Params.Metric);
            double optimalTravelTime = _orderService.GetOrderViews().Sum(ov => metric(ov.GetModel().PickupLocation, ov.GetModel().DeliveryLocation).ToDouble());
            double travelTimeOptimality = 100 * (optimalTravelTime / totalTravelTime);
            _travelTimeOptimalitySeries.Points.Add(new DataPoint(WindowModel.CurrentTime.ToDouble(), travelTimeOptimality));

            // Orders
            double handledOrdersPercent = 100 * (WindowModel.Stats.HandledOrders / (double)WindowModel.Stats.TotalOrders);
            double rejectedOrderPercent = 100 * (WindowModel.Stats.RejectedOrders / (double)WindowModel.Stats.TotalOrders);

            _handledOrdersSeries.Points.Add(new DataPoint(WindowModel.CurrentTime.ToDouble(), handledOrdersPercent));
            _rejectedOrdersSeries.Points.Add(new DataPoint(WindowModel.CurrentTime.ToDouble(), rejectedOrderPercent));

            // Evolution


            // Invalidate plots
            WindowModel.TotalProfitPlot.InvalidatePlot(true);
            WindowModel.OptimalityPlot.InvalidatePlot(true);
            WindowModel.OrdersStatePlot.InvalidatePlot(true);
            WindowModel.EvolutionPlot.InvalidatePlot(true);
        }

        #endregion

        #region Data

        private void SaveData()
        {
            SaveFileDialog sfd = new();
            sfd.DefaultExt = "json";
            sfd.Filter = "JSON Files | *.json";
            sfd.FileName = $"darp_data_{DateTime.Now.ToString("yyyyMMddHHmmss")}.json";
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

        private void LoadData()
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

        #endregion

        #region Plots

        private void InitPlots()
        {
            // Order
            WindowModel.OrdersStatePlot = new PlotModel { Title = "Orders"};
            WindowModel.OrdersStatePlot.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
            });
            WindowModel.OrdersStatePlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100, Unit = "%" });
            WindowModel.OrdersStatePlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Unit = "ticks" });
            _handledOrdersSeries = new LineSeries() { Color = OxyColor.FromRgb(0, 255, 0), Title = "Handled orders" };
            _rejectedOrdersSeries = new LineSeries() { Color = OxyColor.FromRgb(255, 0, 0), Title = "Rejected orders" };
            WindowModel.OrdersStatePlot.Series.Add(_handledOrdersSeries);
            WindowModel.OrdersStatePlot.Series.Add(_rejectedOrdersSeries);
            WindowModel.OrdersStatePlot.InvalidatePlot(true);

            // Optimality
            WindowModel.OptimalityPlot = new PlotModel { Title = "Optimality" };
            WindowModel.OptimalityPlot.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
            });
            WindowModel.OptimalityPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100, Unit = "optimality %" });
            WindowModel.OptimalityPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Unit = "ticks" });
            _profitOptimalitySeries = new LineSeries() { Color = OxyColor.FromRgb(0, 255, 0), Title = "Profit" };
            _travelTimeOptimalitySeries = new LineSeries() { Color = OxyColor.FromRgb(0, 0, 255), Title = "Travel time" };
            WindowModel.OptimalityPlot.Series.Add(_profitOptimalitySeries);
            WindowModel.OptimalityPlot.Series.Add(_travelTimeOptimalitySeries);
            WindowModel.OptimalityPlot.InvalidatePlot(true);

            // Profit
            WindowModel.TotalProfitPlot = new PlotModel { Title = "Total profit" };
            WindowModel.TotalProfitPlot.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
            });
            WindowModel.TotalProfitPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Unit = "profit" });
            WindowModel.TotalProfitPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Unit = "ticks" });
            _totalProfitSeries = new LineSeries() { Color = OxyColor.FromRgb(0, 255, 0), Title = "Profit" };
            WindowModel.TotalProfitPlot.Series.Add(_totalProfitSeries);
            WindowModel.TotalProfitPlot.InvalidatePlot(true);

            // Evolution
            WindowModel.EvolutionPlot = new PlotModel { Title = "Evolution" };
            WindowModel.EvolutionPlot.Legends.Add(new Legend()
            {
                LegendPosition = LegendPosition.RightTop,
            });
            WindowModel.EvolutionPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Unit = "" });
            WindowModel.EvolutionPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Unit = "generation" });
            _evolutionAvgFitnessSeries = new LineSeries() { Color = OxyColor.FromRgb(0, 255, 0), Title = "fitness" };
            WindowModel.EvolutionPlot.Series.Add(_evolutionAvgFitnessSeries);
            WindowModel.EvolutionPlot.InvalidatePlot(true);
        }

        private void SavePlot(PlotModel model)
        {
            SaveFileDialog sfd = new();
            sfd.DefaultExt = "png";
            sfd.Filter = "PNG Files | *.png";
            sfd.FileName = $"darp_plot_{model.Title}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.png";
            if (sfd.ShowDialog() == true)
            {
                var pngExporter = new PngExporter() { Width = 1920, Height = 1080 };
                pngExporter.ExportToFile(model, sfd.FileName);
            }
        }

        private void CopyPlotToClipboard(PlotModel model)
        {
            var pngExporter = new PngExporter { Width = 1920, Height = 1080 };
            var bitmap = pngExporter.ExportToBitmap(model);
            Clipboard.SetImage(bitmap);
        }

        #endregion

        #endregion

        #region EVENT METHODS

      

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(LAYOUT_FILE))
            {
                var layoutSerializer = new XmlLayoutSerializer(dockManager);
                layoutSerializer.Deserialize(LAYOUT_FILE);
            }

            dgOrders.ItemsSource = _orderService.GetOrderViews();
            dgVehicles.ItemsSource = _vehicleService.GetVehicleViews();
            dgSimTemplates.ItemsSource = _mainWindowParams;
            

            pgSettings.ExpandAllProperties();

            LoggerBase.Instance.TextWriters.Add(new TextBoxWriter(txtLog));

            ResetRandom();

            DrawMapLegened();

            InitPlots();


            foreach (var child in dockManager.Layout.Descendents())
            {
                if (child is LayoutAnchorable la)
                {
                    var mi = new MenuItem();
                    mi.IsCheckable = true;
                    mi.IsChecked = la.IsVisible;
                    mi.Header = la.Title;
                    mi.Click += (s1, e1) =>
                    {
                        la.Show();
                    };
                    miView.Items.Add(mi);
                }
            }
        }

        private void newRandomOrder_Click(object sender, RoutedEventArgs e)
        {
            AddRandomOrder();
        }

        private void btwNewRandomVehicle_Click(object sender, RoutedEventArgs e)
        {
            AddRandomVehicle();
        }

        private void btnSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (WindowModel.SimulationState == MainWindowModel.SimulationStateEnum.Ready)
            {
                if (_vehicleService.GetVehicleViews().Count == 0)
                {
                    btnSimulation.IsChecked = false;
                    MessageBox.Show("Add at least one vehicle", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                btnSimulation.IsChecked = true;
                WindowModel.SimulationState = MainWindowModel.SimulationStateEnum.Running;
                btnSimulation.Content = "Stop simulation";

                StartSimulation();
            }
            else
            {
                btnSimulation.IsChecked = false;
                WindowModel.SimulationState = MainWindowModel.SimulationStateEnum.Ready;
                btnSimulation.Content = "Start simulation";

                StopSimulation();
            }
        }


        private void btnTick_Click(object sender, RoutedEventArgs e)
        {
            Tick();
        }

        private void btnSaveData_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
        }

        private void btnLoadData_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void txtLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtLog.ScrollToEnd();
        }

        private void btnRefreshMap_Click(object sender, RoutedEventArgs e)
        {
            DrawManhattanMap();
        }

        private void btnRunInsertion_Click(object sender, RoutedEventArgs e)
        {
            RunInsertionHeuristics();
            RenderPlan();
        }

        private void btnRunMIP_Click(object sender, RoutedEventArgs e)
        {
            RunMIPSolver();
            RenderPlan();
        }

        private void btnRunEvo_Click(object sender, RoutedEventArgs e)
        {
            RunEvolution().ContinueWith(t =>
            {
                Application.Current.Dispatcher.BeginInvoke(() => RenderPlan());
            });
           
        }

        private void btnUpdatePlan_Click(object sender, RoutedEventArgs e)
        {
            UpdatePlan();
            RenderPlan();
        }
        private void btnReserRandom_Click(object sender, RoutedEventArgs e)
        {
            ResetRandom();
        }

        private void btnSavePlot_Click(object sender, RoutedEventArgs e)
        {
            SavePlot((PlotModel)((Control)sender).Tag);
        }

        private void btnExportTimeSeries_Click(object sender, RoutedEventArgs e)
        {
            ExportTimeSeriesCsv();
        }

        private void btnAddOrderToMap_Click(object sender, RoutedEventArgs e)
        {
            AddRandomOrder();
        }

        private void btnAddVehicleToMap_Click(object sender, RoutedEventArgs e)
        {
            AddRandomVehicle();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var layoutSerializer = new XmlLayoutSerializer(dockManager);
            layoutSerializer.Serialize(LAYOUT_FILE);
        }

        private void btnDocsCz_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(MANUAL_CZ))
            {
                Process p = new();
                p.StartInfo = new(MANUAL_CZ) { UseShellExecute = true };
                p.Start();
            }
        }

        private void btnSimTemplates_Click(object sender, RoutedEventArgs e)
        {
            if (WindowModel.SimulationTemplatesState == MainWindowModel.SimulationStateEnum.Ready)
            {
                btnRunSimTemplates.IsChecked = true;
                WindowModel.SimulationTemplatesState = MainWindowModel.SimulationStateEnum.Running;

                RunSimulationTemplates();
            }
            else
            {
                btnRunSimTemplates.IsChecked = false;
                WindowModel.SimulationTemplatesState = MainWindowModel.SimulationStateEnum.Ready;
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnSaveParamsAsTemplate_Click(object sender, RoutedEventArgs e)
        {
            SaveParamsAsSimulationTemplate();
        }

        #endregion
    }

    #region CLASSES


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
        public SimulationStateEnum SimulationState { get; set; }
        public SimulationStateEnum SimulationTemplatesState { get; set; }
        public MainWindowStats Stats { get; set; } = new();
        public MainWindowParams Params { get; set; } = new();
        
        [JsonIgnore]
        public PlotModel OrdersStatePlot { get; set; }
        [JsonIgnore]
        public PlotModel TotalProfitPlot { get; set; }
        [JsonIgnore]
        public PlotModel OptimalityPlot { get; set; }
        [JsonIgnore]
        public PlotModel EvolutionPlot { get; set; }

        public enum SimulationStateEnum
        {
            Ready = 0,
            Running = 1,
        }

        public MainWindowModel Clone()
        {
            MainWindowModel clone = MemberwiseClone() as MainWindowModel;
            clone.Params = Params.Clone();
            clone.Stats = Stats.Clone();
            clone.OrdersStatePlot = new();
            clone.TotalProfitPlot = new();
            clone.OptimalityPlot = new();
            clone.EvolutionPlot = new();

            return clone;
        }
    }

    [AddINotifyPropertyChangedInterface]
    internal class MainWindowStats
    {
        public int TotalOrders { get; set; } = 1;
        public int HandledOrders { get; set; }
        public int AcceptedOrders { get; set; }
        public int RejectedOrders { get; set; }
        public double CurrentProfit { get; set; }
        public double TotalProfit { get; set; }

        public string CurrentProfitStr { get => $"Current profit: {CurrentProfit}"; }
        public string TotalProfitStr { get => $"Total profit: {TotalProfit}"; }
        public string TotalOrdersStr { get => $"Total orders: {TotalOrders}"; }
        public string AcceptedOrdersStr { get => $"Accepted orders: {AcceptedOrders} ({100 * AcceptedOrders / TotalOrders}%)"; }
        public string HandledOrdersStr { get => $"Handled orders: {HandledOrders} ({100 * HandledOrders / TotalOrders}%)"; }
        public string RejectedOrdersStr { get => $"Rejected orders: {RejectedOrders} ({100 * RejectedOrders / TotalOrders}%)"; }

        public MainWindowStats Clone()
        {
            return MemberwiseClone() as MainWindowStats;
        }

    }

    internal class MainWindowParams
    {
        // ------------ Order generation ------------------
        [Category("Order generation")]
        [DisplayName("Delivery time")]
        [Description("Delivery time since current time.")]
        [ExpandableObject]
        public PropertyRange<int> MaxDeliveryTimeFrom { get; set; } = new(30, 60);

        [Category("Order generation")]
        [DisplayName("Time window ticks")]
        [Description("")]
        public double OrderTimeWindowTicks { get; set; } = 3;

        [Category("Order generation")]
        [DisplayName("Profit")]
        [Description("Profit per tick.")]
        public double OrderProfitPerTick { get; set; } = 3;

        // ------------ Randomization ------------------
        [Category("Randomization")]
        public int Seed { get; set; } = (int)DateTime.Now.Ticks;

        // ------------ Time series ------------------
        [Category("Time series")]
        [DisplayName("Update rate [ticks]")]
        [Description("Update time series each [n] ticks.")]
        public int TimeSeriesUpdateEachTicks { get; set; } = 1;

        // ------------ Simulation ------------------
        [Category("Simulation")]
        [DisplayName("Update plan each [ticks]")]
        public int UpdatePlanTicks { get; set; } = 10;

        [Category("Simulation")]
        [DisplayName("New orders each [ticks]")]
        public int GenerateNewOrderTicks { get; set; } = 2;

        [Category("Simulation")]
        [DisplayName("Tick each [seconds]")]
        public int TickEach { get; set; } = 1;

        [Category("Simulation")]
        [DisplayName("Expected orders count")]
        [Description("[ExpectedOrdersCount] * [OrdersCountVariance] orders is generated independently with probability 1 / [OrdersCountVariance].")]
        public int ExpectedOrdersCount { get; set; } = 1;

        [Category("Simulation")]
        [DisplayName("Orders count variance")]
        [Description("[ExpectedOrdersCount] * [OrdersCountVariance] orders is generated independently with probability 1 / [OrdersCountVariance].")]
        public int OrdersCountVariance { get; set; } = 5;

        [Category("Simulation")]
        [DisplayName("Optimization method")]
        public OptimizationMethod OptimizationMethod { get; set; } = OptimizationMethod.Evolutionary;

        [Category("Simulation")]
        [DisplayName("Use insertion heuristics")]
        public bool UseInsertionHeuristics { get; set; } = false;

        [Category("Simulation")]
        [DisplayName("[Temmplate] Total ticks")]
        public int TemplateTotalTicks { get; set; } = 120;

        [Category("Simulation")]
        [DisplayName("[Temmplate] Total runs")]
        public int TemplateTotalRuns { get; set; } = 1;

        [Category("Simulation")]
        [DisplayName("[Temmplate] Vehicles count")]
        public int TemplateVehiclesCount { get; set; } = 10;

        [Category("Simulation")]
        [DisplayName("[Temmplate] Name")]
        public string TemplateName { get; set; } = "1";

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
        [DisplayName("Charge per tick")]
        [Description("Charge per tick of drive.")]
        public int VehicleChargePerTick { get; set; } = 1;


        // ------------ MIP solver ------------------
        [Category("MIP solver")]
        [DisplayName("Time limit [miliseconds]")]
        [Description("MIP solver time limit in miliseconds. If set to zero, then solving time is unlimited.")]
        public int MIPTimeLimit { get; set; } = 60_000;

        [Category("MIP solver")]
        [DisplayName("Multithreading")]
        [Description("Enable multithreading for MIP solver. Uses half of available threads.")]
        public bool MIPMultithreading { get; set; } = false;

        [Category("MIP solver")]
        [DisplayName("Internal solver")]
        [Description("Internal solver. The only available is the SCIP.")]
        public string Solver { get; set; } = "SCIP";

        [Category("MIP solver")]
        [DisplayName("Objective")]
        public OptimizationObjective MIPObjective { get; set; } = OptimizationObjective.MaximizeProfit;


        // ------------ Evolution ------------------
        [Category("Evolution")]
        [DisplayName("Generations")]
        public int EvoGenerations { get; set; } = 200;

        [Category("Evolution")]
        [DisplayName("Population size")]
        public int EvoPopSize { get; set; } = 100;

        [Category("Evolution")]
        [DisplayName("[MUT] Remove order prob.")]
        public double RandomOrderRemoveMutProb { get; set; } = 0.4;

        [Category("Evolution")]
        [DisplayName("[MUT] Insert order prob.")]
        public double RandomOrderInsertMutProb { get; set; } = 0.7;

        [Category("Evolution")]
        [DisplayName("[MUT] BestFit order prob.")]
        public double BestfitOrderInsertMutProb { get; set; } = 0.7;

        [Category("Evolution")]
        [DisplayName("[CX] Plan crossover")]
        public double PlanCrossoverProb { get; set; } = 0.3;

        [Category("Evolution")]
        [DisplayName("[CX] Route crossover")]
        public double RouteCrossoverProb { get; set; } = 0.3;

        [Category("Evolution")]
        [DisplayName("Enviromental selection")]
        public EnviromentalSelection EnviromentalSelection { get; set; } = EnviromentalSelection.Tournament;

        // ------------ Insertion heuristics ------------------
        [Category("Insertion heuristics")]
        [DisplayName("Mode")]
        [Description("Insertion heuristics mode. A First fit inserts a order into first route found. A Best fit finds the most tight space where the order fits. Best fit might be slightly slower than First fit.")]
        public InsertionHeuristicsMode InsertionMode { get; set; } = InsertionHeuristicsMode.FirstFit;

        public MainWindowParams Clone()
        {
            return MemberwiseClone() as MainWindowParams;
        }
    }

    internal struct PropertyRange<T>
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

    #endregion
}
