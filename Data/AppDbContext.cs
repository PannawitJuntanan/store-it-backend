using System.Transactions;
using Microsoft.EntityFrameworkCore;
using StoreItApi.Models;

namespace StoreItApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemTransaction> Transactions { get; set; }

        // ตรงนี้เอาไว้ Config เพิ่มเติมถ้าต้องการ
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Map ตาราง Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users"); // ชื่อ Table ใน SQL Server
                entity.HasKey(e => e.Id); // บอกว่าเป็น Primary Key

                // ถ้าชื่อ Field ใน Class ไม่ตรงกับ Column ใน DB ให้แก้ตรงนี้
                // entity.Property(e => e.Username).HasColumnName("Username"); 
            });

            // 2. Map ตาราง Items
            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Items");
                entity.HasKey(e => e.Id);

                // กำหนดให้ RFID เป็น Unique Index (ตาม SQL)
                entity.HasIndex(e => e.RfidTag).IsUnique();
            });

            // 3. Map ตาราง Transactions (สำคัญเรื่อง Foreign Key)
            modelBuilder.Entity<ItemTransaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasKey(e => e.Id);

                // Config ความสัมพันธ์ (Relationships)
                // เพื่อให้เวลา Query สามารถ .Include(t => t.Item) ได้
                entity.HasOne(t => t.Item)
                      .WithMany() // 1 Item มีหลาย Transaction ได้ (หรือใส่ WithMany(i => i.Transactions) ถ้าอยากเรียกกลับ)
                      .HasForeignKey(t => t.ItemId);

                entity.HasOne(t => t.Requester)
                      .WithMany()
                      .HasForeignKey(t => t.RequesterId);
            });
        }
    }
}