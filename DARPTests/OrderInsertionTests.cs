using DARP.Models;
using DARP.Services;
using DARP.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace DARPTests
{
    [TestClass]
    public class OrderInsertionTests
    {
        private readonly IPlanningService _planningService;

        public OrderInsertionTests()
        {
            _planningService = ServiceProvider.Default.GetService<IPlanningService>();
        }

        [TestMethod]
        public void InsertOrderIntoEmptyRoute()
        {
            _planningService.Init(new Plan(XMath.ManhattanMetric));
            _planningService.AddVehicle(Time.Zero, new Vehicle() { Location = new Cords(3, 3) });

            Time currentTime = Time.Zero;
            List<Order> newOrders = new()
            {
                new Order() { PickupLocation = new(1,1), DeliveryLocation = new(10,10), DeliveryTimeWindow = new(new(10), new(30)) }
            };
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count); // Exactly one route
            Assert.AreEqual(3, _planningService.Plan.Routes[0].Points.Count); // Three points at route
        }

        [TestMethod]
        public void AppendOrderToRoute()
        {
            _planningService.Init(new Plan(XMath.ManhattanMetric));
            _planningService.AddVehicle(Time.Zero, new Vehicle() { Location = new Cords(3, 3) });

            Time currentTime = Time.Zero;
            List<Order> newOrders = new()
            {

                new Order() { Id = 1, PickupLocation = new(1,1), DeliveryLocation = new(10,10), DeliveryTimeWindow = new(new(10), new(30)) },
                new Order() { Id = 2, PickupLocation = new(8,8), DeliveryLocation = new(4,4), DeliveryTimeWindow = new(new(40), new(80)) },
            };
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count);
            Assert.AreEqual(5, _planningService.Plan.Routes[0].Points.Count);
            Assert.AreEqual(1, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[2]).Order.Id);
            Assert.AreEqual(2, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[4]).Order.Id);

        }

        [TestMethod]
        public void PrependOrderToRoute()
        {
            _planningService.Init(new Plan(XMath.ManhattanMetric));
            _planningService.AddVehicle(Time.Zero, new Vehicle() { Location = new Cords(3, 3) });

            Time currentTime = Time.Zero;
            List<Order> newOrders = new()
            {
                
                new Order() { Id = 1, PickupLocation = new(8,8), DeliveryLocation = new(4,4), DeliveryTimeWindow = new(new(40), new(80)) },
                new Order() { Id = 2, PickupLocation = new(1,1), DeliveryLocation = new(10,10), DeliveryTimeWindow = new(new(10), new(30)) },
            };
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count);
            Assert.AreEqual(5, _planningService.Plan.Routes[0].Points.Count);
            Assert.AreEqual(2, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[2]).Order.Id);
            Assert.AreEqual(1, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[4]).Order.Id);
        }

        [TestMethod]
        public void InsertOrderIntoRoute()
        {
            _planningService.Init(new Plan(XMath.ManhattanMetric));
            _planningService.AddVehicle(Time.Zero, new Vehicle() { Location = new Cords(3, 3) });

            Time currentTime = Time.Zero;
            List<Order> newOrders = new()
            {

                new Order() { Id = 1, PickupLocation = new(8,8), DeliveryLocation = new(4,4), DeliveryTimeWindow = new(new(90), new(110)) },
                new Order() { Id = 2, PickupLocation = new(1,1), DeliveryLocation = new(10,10), DeliveryTimeWindow = new(new(10), new(30)) },
                new Order() { Id = 3, PickupLocation = new(7,7), DeliveryLocation = new(5,5), DeliveryTimeWindow = new(new(70), new(90)) },
            };
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count);
            Assert.AreEqual(7, _planningService.Plan.Routes[0].Points.Count);
            Assert.AreEqual(2, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[2]).Order.Id);
            Assert.AreEqual(3, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[4]).Order.Id);
            Assert.AreEqual(1, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[6]).Order.Id);
        }

        [TestMethod]
        public void MoveVehicle()
        {
            _planningService.Init(new Plan(XMath.ManhattanMetric));
            _planningService.AddVehicle(Time.Zero, new Vehicle() { Location = new Cords(0, 0) });

            Time currentTime = Time.Zero;
            List<Order> newOrders = new()
            {
                new Order() { Id = 1, PickupLocation = new(1,1), DeliveryLocation = new(4,4), DeliveryTimeWindow = new(new(6), new(10)) },
            };
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count);
            Assert.AreEqual(3, _planningService.Plan.Routes[0].Points.Count);
            Assert.AreEqual(1, ((OrderDeliveryRoutePoint)_planningService.Plan.Routes[0].Points[2]).Order.Id);

            currentTime = new Time(5);
            newOrders = new();
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count);
            Assert.AreEqual(1, _planningService.Plan.Routes[0].Points.Count);
            Assert.AreEqual(new Time(8), ((VehicleRoutePoint)_planningService.Plan.Routes[0].Points[0]).Time);
        }
    }
}