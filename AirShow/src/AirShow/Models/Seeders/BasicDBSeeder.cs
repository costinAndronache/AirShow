using AirShow.Models.Contexts;
using AirShow.Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Seeders
{
    public class BasicDBSeeder
    {
        private AirShowContext _context;

        public BasicDBSeeder(AirShowContext ctx)
        {
            _context = ctx;
        }

        public void Run()
        {
            if (!_context.Categories.Any())
            {
                PopulateCategories();
            }
        }

        private void PopulateCategories()
        {
            // to-do -- must use a config file
            string[] categoriesNames = new string[] {"Education", "Software", "Sport"};
            foreach (var item in categoriesNames)
            {
                Category category = new Category
                {
                    Name = item
                };

                _context.Categories.Add(category);
            }

            _context.SaveChanges();
        }
    }
}
