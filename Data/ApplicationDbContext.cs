using E_mob_shoppy.Models;
using Microsoft.EntityFrameworkCore;

namespace E_mob_shoppy.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(new Category { Category_Id = 1, Name = "Arun", Description = "HHhihkj", DisplayOrder = 1 },
                new Category { Category_Id = 2, Name = "Arun", Description = "HHhihkj", DisplayOrder = 1 },
                new Category { Category_Id = 3, Name = "Arun", Description = "HHhihkj", DisplayOrder = 1 }
                );
        }
    }
}
