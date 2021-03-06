﻿using System.Collections.Generic;

namespace Railway.Web.Models.Railway
{
    public class SelectRouteViewModel
    {
        public IList<List<string>> AvailableRoutes { get; set; }

        public SelectRouteFormModel FormModel { get; set; }
    }
}