using AirShow.Models.Common;
using AirShow.Models.Contexts;
using AirShow.Models.EF;
using AirShow.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.AppRepositories
{
    public class EFCategoriesRepository: ICategoriesRepository
    {
        private AirShowContext _context;

        public EFCategoriesRepository(AirShowContext context)
        {
            _context = context;
        }

        public async Task<OperationResult<Category>> GetCategoryForPresentation(Presentation p)
        {
            var list =  _context.Categories.Where(c => c.Id == p.CategoryId).ToList();
            if (list.Count == 0)
            {
                return new OperationResult<Category>
                {
                    ErrorMessageIfAny = "Category not found"
                };
            }

            return new OperationResult<Category>
            {
                Value = list.First()
            };
        }

        public async Task<OperationResult<List<Category>>> GetCurrentCategories()
        {
            return new OperationResult<List<Category>>
            {
                Value = _context.Categories.ToList()
            };
        }
    }
}
