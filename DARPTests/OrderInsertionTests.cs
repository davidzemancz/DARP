using DARP.Models;
using DARP.Services;
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
        public void InsertIntoEmptyRoute()
        {
            _planningService.InitPlan(
                travelTime: (c1, c2) => new Time((int)(Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y))),
                vehicles: new List<Vehicle>() { new Vehicle() { Location = new Cords(3,3) } }
                );

            Time currentTime = new(1);
            List<Order> newOrders = new()
            {
                new Order() { PickupLocation = new(1,1), DeliveryLocation = new(10,10), DeliveryTimeWindow = new(new(10), new(30)) }
            };
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count); // Exactly one route
            Assert.AreEqual(3, _planningService.Plan.Routes[0].Points.Count); // Three points at route
        }

        [TestMethod]
        public void InsertIntoRoute()
        {
            _planningService.InitPlan(
                travelTime: (c1, c2) => new Time((int)(Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y))),
                vehicles: new List<Vehicle>() { new Vehicle() { Location = new Cords(3, 3) } }
                );

            Time currentTime = new(1);
            List<Order> newOrders = new()
            {
                new Order() { PickupLocation = new(1,1), DeliveryLocation = new(10,10), DeliveryTimeWindow = new(new(10), new(30)) },
                new Order() { PickupLocation = new(8,8), DeliveryLocation = new(4,4), DeliveryTimeWindow = new(new(40), new(80)) }
            };
            _planningService.UpdatePlan(currentTime, newOrders);

            Assert.AreEqual(1, _planningService.Plan.Routes.Count); // Exactly one route
            Assert.AreEqual(5, _planningService.Plan.Routes[0].Points.Count); // Three points at route
        }
    }
}