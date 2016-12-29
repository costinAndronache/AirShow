using AirShow.Models.EF;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirShow.Models.Contexts
{
    public class AirShowContext: IdentityDbContext
    {
        public AirShowContext(DbContextOptions options): base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PresentationTag>()
                .HasKey(t => new { t.PresentationId, t.TagId });

            modelBuilder.Entity<PresentationTag>()
                .HasOne(pt => pt.Presentation)
                .WithMany(p => p.PresentationTags)
                .HasForeignKey(pt => pt.PresentationId);

            modelBuilder.Entity<PresentationTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PresentationTags)
                .HasForeignKey(pt => pt.TagId);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Presentation> Presentations { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set;}
        public DbSet<PresentationTag> PresentationTags { get; set; }
    }
}
