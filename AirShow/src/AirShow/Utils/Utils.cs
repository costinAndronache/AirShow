using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Utils
{
    public class Utils
    {
        public static string ControllerName(Type controllerType)
        {
            return nameof(controllerType).Replace("Controller", "");
        }
    }
}
