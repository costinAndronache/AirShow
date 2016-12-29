using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Utils
{
    public static class StringExtensions
    {
        public static string WithoutControllerPart(this string s)
        {
            return s.Replace("Controller", "");
        }
    }
    
}
