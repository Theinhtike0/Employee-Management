using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HR_Products.Models.Entitites;
using HR_Products.Models;

namespace HR_Products.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // Constructor remains unchanged
        }

        // DbSet definitions for your custom entities
        public DbSet<EmployeeProfile> EMPE_PROFILE { get; set; }
        public DbSet<LeaveType> LEAV_TYPE { get; set; }
        public DbSet<Leavescheme> LEAV_SCHEME { get; set; }
        public DbSet<Leaveschemetype> LEAV_SCHEME_TYPE { get; set; }
        public DbSet<Leaveschemetypedetl> LEAV_SCHEME_TYPE_DETL { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Leavescheme (Primary Key)
            modelBuilder.Entity<Leavescheme>()
                .HasKey(s => s.SCHEME_ID);

            modelBuilder.Entity<Leavescheme>()
                .Property(s => s.SCHEME_ID)
                .ValueGeneratedOnAdd();

            // Leaveschemetype (Primary Key)
            modelBuilder.Entity<Leaveschemetype>()
                .HasKey(t => t.TYPE_ID);

            modelBuilder.Entity<Leaveschemetype>()
                .Property(t => t.TYPE_ID)
                .ValueGeneratedOnAdd();

            // Leaveschemetypedetl (Primary Key)
            modelBuilder.Entity<Leaveschemetypedetl>()
                .HasKey(d => d.DETL_ID);

            modelBuilder.Entity<Leaveschemetypedetl>()
                .Property(d => d.DETL_ID)
                .ValueGeneratedOnAdd();

            // 1 Leavescheme → many Leaveschemetype
            modelBuilder.Entity<Leaveschemetype>()
                .HasOne(t => t.LEAV_SCHEME)
                .WithMany(s => s.LEAV_SCHEME_TYPE)
                .HasForeignKey(t => t.SCHEME_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // 1 LeaveType → many Leaveschemetype
            modelBuilder.Entity<Leaveschemetype>()
                .HasOne(t => t.LEAVE_TYPE)
                .WithMany() // or create navigation in LeaveType if needed
                .HasForeignKey(t => t.LEAVE_TYPE_ID)
                .OnDelete(DeleteBehavior.Restrict); // Use Restrict if you don’t want cascade delete here

            // 1 Leaveschemetype → many Leaveschemetypedetl
            modelBuilder.Entity<Leaveschemetypedetl>()
                .HasOne(d => d.LEAV_SCHEME_TYPE)
                .WithMany(t => t.LEAV_SCHEME_TYPE_DETL)
                .HasForeignKey(d => d.TYPE_ID)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}