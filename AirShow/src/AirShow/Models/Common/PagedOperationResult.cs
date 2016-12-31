using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Common
{
    public class PagedOperationResult<T>: OperationResult<T>
    {
        public int PagesLeft { get; set; }
        public int ItemsPerPage { get; set; }
    }
}
