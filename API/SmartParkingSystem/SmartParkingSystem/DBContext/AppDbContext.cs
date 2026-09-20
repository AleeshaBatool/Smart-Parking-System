using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Models;
using System.Collections.Generic;

namespace SmartParkingSystem.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
       
        public DbSet<Parking> Parkings { get; set; }
        public DbSet<CarParking> CarParkings { get; set; }
    }
}
