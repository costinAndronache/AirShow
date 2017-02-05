using AirShow.Models.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AirShow.Models.Cleaners
{
    public class UnconfirmedAccountsCleaner
    {
        private static int CleaningIntervalSeconds = 24 * 3600;
        private static int CleaningIntervalMiliseconds = CleaningIntervalSeconds * 1000;
        private AirShowContext _context;
        private Timer _timer;

        public UnconfirmedAccountsCleaner(AirShowContext context)
        {
            _context = context;
        }

        public void Run()
        {
            _timer = new Timer(async (obj) =>
            {
                var time = DateTime.Now;
                var unconfirmedAccounts = await _context.Users.Where(u => !u.EmailConfirmed &&
               (time - u.CreationDate).TotalSeconds >= CleaningIntervalSeconds).ToListAsync();

                if (unconfirmedAccounts != null && unconfirmedAccounts.Count() > 0)
                {
                    _context.Users.RemoveRange(unconfirmedAccounts);
                    await _context.SaveChangesAsync();
                }

            }, null, CleaningIntervalMiliseconds, CleaningIntervalMiliseconds);
        }
    }
}
