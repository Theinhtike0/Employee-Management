using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HR_Products.Models.Entitites;
using HR_Products.Models;
using HR_Products.Models.Entities;

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
        public DbSet<LeaveBalance> LEAV_BALANCE { get; set; }
        public DbSet<Holiday> HOLIDAYS { get; set; }
        public DbSet<LeaveRequest> LEAV_REQUESTS { get; set; }



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

            modelBuilder.Entity<LeaveBalance>()
    .HasKey(lb => lb.Id);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(lb => lb.Employee)
                .WithMany()
                .HasForeignKey(lb => lb.EmpeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(lb => lb.LeaveType)
                .WithMany()
                .HasForeignKey(lb => lb.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.CreatedDate)
            .HasDefaultValueSql("GETDATE()");
                
            modelBuilder.Entity<LeaveType>()
                .ToTable("LEAV_TYPE");

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Employee)
                .WithMany()
                .HasForeignKey(lr => lr.EmployeeId);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.LeaveType)
                .WithMany()
                .HasForeignKey(lr => lr.LeaveTypeId);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Approver)
                .WithMany()
                .HasForeignKey(lr => lr.ApprovedById);

            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                entity.Property(lr => lr.Duration)
                    .HasColumnType("decimal(18,2)");

                entity.Property(lr => lr.LeaveBalance)
                    .HasColumnType("decimal(18,2)");

                entity.Property(lr => lr.UsedToDate)
                    .HasColumnType("decimal(18,2)");

                entity.Property(lr => lr.AccrualBalance)
                    .HasColumnType("decimal(18,2)");
            });


            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                // Other configurations...

                entity.HasOne(lr => lr.Approver)
                      .WithMany()
                      .HasForeignKey(lr => lr.ApprovedById)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false)  // This makes the relationship optional
                      .HasConstraintName("FK_LEAV_REQUESTS_EMPE_PROFILE_ApprovedById");
            });

            modelBuilder.Entity<EmployeeProfile>()
             .Property(e => e.UserGuid)
             .HasDefaultValueSql("NEWID()");


        }

    }
}