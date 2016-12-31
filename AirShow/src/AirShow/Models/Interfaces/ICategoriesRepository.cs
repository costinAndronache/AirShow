using AirShow.Models.Common;
using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Interfaces
{
    public interface ICategoriesRepository
    {
        Task<OperationResult<List<Category>>> GetCurrentCategories();
    }
}
