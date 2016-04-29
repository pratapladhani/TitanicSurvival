using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TitanicSurvivalPredictAPI.Models
{
    public enum Sex { Male, Female }
    public class SurvivalPrediction
    {
        public int Age { get; set; }
        public string Sex { get; set; }
        public bool WillSurvive { get; set; }
        public double SurvivalProbability { get; set; }
    }
}