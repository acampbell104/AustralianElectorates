﻿using System;
using System.Collections.Generic;

namespace AustralianElectorates
{
    public class Electorate
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public State State { get; set; }
        public double Area { get; set; }
        public bool ExistInCurrent { get; set; }
        public bool ExistInFuture { get; set; }
        public DateTime? DateGazetted { get; set; }
        public string Description { get; set; }
        public string DemographicRating { get; set; }
        public string ProductsAndIndustry { get; set; }
        public string NameDerivation { get; set; }
        public List<Member> Members { get; set; }
    }
}