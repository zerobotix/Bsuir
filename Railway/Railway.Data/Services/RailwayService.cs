﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Railway.Data.Entities;

namespace Railway.Data.Services
{

    public class RailwayService
    {
        private readonly RailwayContext context;

        public RailwayService(RailwayContext context)
        {
            this.context = context;
        }

        public IList<CarType> GetAllCarTypes()
        {
            return context.CarTypes.ToList();
        }

        public IList<Car> GetAllCars(int dailyRouteId) // methos should also check destination and department stations ids
        {
            return context.DailyRoutes.Find(dailyRouteId).Route.Train.Cars.ToList();
        }

        public Station GetStation(string name)
        {
            return context.Stations.FirstOrDefault(x => x.StationName == name); // case insensitive
        }

        public Station GetStation(int id)
        {
            return context.Stations.Find(id);
        }

        public DailyRoute GetDailyRoute(int id)
        {
            return context.DailyRoutes.Find(id);
        }

        public List<DailyRoute> GetDailyRoutes(DateTime date, int startStationId, int destinationStationId)
        {
            var result = new List<DailyRoute>();
            var dailyRoutes = context.DailyRoutes.Where(x => x.StartDateTime >= date.Date).ToList(); // берём все ежедневные маршруты не раньше указанной даты
            foreach (var dailyRoute in dailyRoutes)
            {
                var startRouteStation = dailyRoute.Route.RouteStations.FirstOrDefault(x => x.StationId == startStationId);
                var destinationRouteStation = dailyRoute.Route.RouteStations.FirstOrDefault(x => x.StationId == destinationStationId);

                if (startRouteStation != null && destinationRouteStation != null // если на маршруте есть обе указанные станции
                    && startRouteStation.StationOrder < destinationRouteStation.StationOrder // и станция назначения идёт после станции отправления
                    && dailyRoute.StartDateTime.AddMinutes(startRouteStation.MinutesFromStart) < date.Date.AddDays(1)) // и со станции отправления поезд отправляется в указанную дату
                {
                    result.Add(dailyRoute);
                }
            }


            return result;
        }

        public DateTime CalculateArrivalDepartureTime(DailyRoute dailyRoute, int stationId)
        {
            var routeStation = dailyRoute.Route.RouteStations.First(x => x.StationId == stationId);
            return dailyRoute.StartDateTime.AddMinutes(routeStation.MinutesFromStart);
        }

        public double CalculateCost(int dailyRouteId, int startStationId, int destinationStationId, CarType carType)
        {
            var dailyRoute = context.DailyRoutes.Find(dailyRouteId);
            return CalculateCost(dailyRoute, startStationId, destinationStationId, carType);
        }

        public double CalculateCost(DailyRoute dailyRoute, int startStationId, int destinationStationId, CarType carType)
        {
            double cost = 0;
            var allRouteStations = dailyRoute.Route.RouteStations.ToList();
            var startRouteStation = allRouteStations.First(x => x.StationId == startStationId);
            var destinationRouteStation = allRouteStations.First(x => x.StationId == destinationStationId);
            var routeStations = allRouteStations
                .Where(x => x.StationOrder >= startRouteStation.StationOrder 
                    && x.StationOrder <= destinationRouteStation.StationOrder).ToList();

            for (var i = 0; i < routeStations.Count - 1; i++)
            {
                var firstStationId = routeStations[i].StationId;
                var secondStationId = routeStations[i + 1].StationId;
                var distance = context.Distances.FirstOrDefault(x => x.StartStationId == firstStationId && x.DestinationStationId == secondStationId);
                if (distance == null)
                {
                    cost += 1 * carType.CostRate;
                }
                else
                {
                    cost += Math.Ceiling(distance.DistanceValue * carType.CostRate);
                }
            }

            return cost;
        }

        public int CalculateAvailableSeats(DailyRoute dailyRoute, int carTypeId)
        {
            return dailyRoute.Route.Train.Cars.Where(x => x.CarTypeId == carTypeId).Sum(x => CalculateAvailableSeats(x));
        }

        public int CalculateAvailableSeats(Car car)
        {
            return  car.CarType.SeatCount - context.OrderPassengers.Count(x => x.CarId == car.CarId);
        }

        public IList<int> GetAvailableSeatNumbers(int carId)
        {
            var car = context.Cars.Find(carId);
            return GetAvailableSeatNumbers(car);
        }

        public IList<int> GetAvailableSeatNumbers(Car car)
        {
            var takenSeats = context.OrderPassengers.Where(x => x.CarId == car.CarId).Select(x => x.SeatIndex).ToList();
            var result = Enumerable.Range(1, car.CarType.SeatCount).Where(x => !takenSeats.Contains(x)).ToList();

            return result;
        }

        public IList<List<string>> GetAvailableRoutes()
        {
            var result = new List<List<string>>();

            var routes = context.Routes.ToList();
            foreach (var route in routes)
            {

                var routeStations = context.RouteStations.Where(x => x.RouteId == route.RouteId).OrderBy(x => x.StationOrder);
                var resultStations = routeStations.Select(station => station.Station.StationName).ToList();
                result.Add(resultStations);
            }

            return result;
        }

        public IList<Passenger> GetPassengers(int userId)
        {
            return context.Passengers.Where(x => x.UserId == userId).ToList();
        }

        public IList<IdentificationType> GetIdentificationTypes()
        {
            return context.IdentificationTypes.ToList();
        }

        public Passenger SavePassenger(Passenger passenger)
        {
            var existingPassenger = context.Passengers.FirstOrDefault(x => x.FirstName == passenger.FirstName
                && x.LastName == passenger.LastName
                && x.IdentificationTypeId == passenger.IdentificationTypeId 
                && x.IdentificationNumber == passenger.IdentificationNumber
                && x.UserId == passenger.UserId);

            if (existingPassenger == null)
            {
                context.Passengers.Add(passenger);
                context.SaveChanges();
                return passenger; // после сохранения заполнится PassengerId
            }

            return existingPassenger;
        }

        public Order CreateOrder(int dailyRouteId, int startStationId, int destinationStationId, int carId, int passengerId, int minSeat, int maxSeat)
        {
            var dailyRoute = context.DailyRoutes.Find(dailyRouteId);
            var car = context.Cars.Find(carId);

            //todo: lock -> сохранение заказа с указанным местом

            var seatIndex = GetAvailableSeatNumbers(car).FirstOrDefault(x => x >= minSeat && x <= maxSeat);

            if (seatIndex == 0)
            {
                return null;
            }

            var orderPassenger = new OrderPassenger
            {
                CarId = car.CarId,
                Cost = CalculateCost(dailyRoute, startStationId, destinationStationId, car.CarType),
                PassengerId = passengerId,
                SeatIndex = seatIndex
            };

            var orderPassengers = new List<OrderPassenger> { orderPassenger };

            var order = new Order
            {
                UserId = Constants.DefaultUserId,
                TrainNumber = dailyRoute.Route.Train.TrainNumber,
                DailyRouteId = dailyRoute.DailyRouteId,
                TripStartStationId = startStationId,
                TripDestinationStationId = destinationStationId,
                TripStartDate = CalculateArrivalDepartureTime(dailyRoute, startStationId),
                TripDestinationDate = CalculateArrivalDepartureTime(dailyRoute, destinationStationId),
                OrderDate = DateTime.Now,
                TotalCost = orderPassengers.Select(x => x.Cost).Sum(),
                OrderPassengers = orderPassengers
            };

            context.Orders.Add(order);

            context.SaveChanges();

            return order;
        }

        public Order GetOrder(int orderId)
        {
            return
                context.Orders.Where(x => x.OrderId == orderId)
                    .Include(x => x.OrderPassengers.Select(y => y.Car))
                    .Include(x => x.TripStartStation)
                    .Include(x => x.TripDestinationStation)
                    .FirstOrDefault();
        }
    }
}
