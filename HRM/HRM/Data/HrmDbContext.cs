using HRM.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Data
{
    public class HrmDbContext : DbContext
    {
        public HrmDbContext(DbContextOptions<HrmDbContext> options)
            : base(options)
        {
        }

        public DbSet<NhanVien> NhanViens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NhanVien>(entity =>
            {
                entity.HasKey(e => e.Id_NV);

                entity.Property(e => e.Id_NV)
                      .ValueGeneratedOnAdd();
            });
        }
    }
}
