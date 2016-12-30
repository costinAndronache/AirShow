using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Common
{
    public class OperationResult<T> : OperationStatus
    {
        public T Value { get; set; }
    }
}
