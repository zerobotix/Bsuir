﻿using System;
using System.Collections.Generic;

namespace Railway.Web.Models.Railway
{
    public class SelectPassengerViewModel
    {
        public string StartStationName { get; set; }

        public string DepartureStationName { get; set; }

        public string TrainNumber { get; set; }

        public DateTime DepartureTime { get; set; }

        public DateTime ArrivalTime { get; set; }

        public IList<PassengerData> SavedPassengers { get; set; }

        public class PassengerData
        {
            public string Name { get; set; }
        }
    }
}