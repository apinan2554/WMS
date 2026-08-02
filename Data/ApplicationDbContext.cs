using Microsoft.EntityFrameworkCore;
using WH_Logistic.Models;

namespace WH_Logistic.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<ProductMaster> Products => Set<ProductMaster>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
        public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
        public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
        public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
        public DbSet<CycleCount> CycleCounts => Set<CycleCount>();
        public DbSet<CycleCountItem> CycleCountItems => Set<CycleCountItem>();
        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
        public DbSet<PickingList> PickingLists => Set<PickingList>();
        public DbSet<PickingListItem> PickingListItems => Set<PickingListItem>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductMaster>()
                .HasIndex(p => p.SKU).IsUnique();

            modelBuilder.Entity<Zone>()
                .HasIndex(z => z.ZoneCode).IsUnique();

            modelBuilder.Entity<Location>()
                .HasIndex(l => l.BinCode).IsUnique();

            modelBuilder.Entity<StockTransfer>()
                .HasOne(s => s.FromLocation)
                .WithMany()
                .HasForeignKey(s => s.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(s => s.ToLocation)
                .WithMany()
                .HasForeignKey(s => s.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data
            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { CategoryId = 1, CategoryName = "อิเล็กทรอนิกส์" },
                new ProductCategory { CategoryId = 2, CategoryName = "อาหารและเครื่องดื่ม" },
                new ProductCategory { CategoryId = 3, CategoryName = "เครื่องใช้สำนักงาน" }
            );

            modelBuilder.Entity<Zone>().HasData(
                new Zone { ZoneId = 1, ZoneCode = "ZONE-A", ZoneName = "โซน A - อิเล็กทรอนิกส์", CategoryId = 1 },
                new Zone { ZoneId = 2, ZoneCode = "ZONE-B", ZoneName = "โซน B - อาหารและเครื่องดื่ม", CategoryId = 2 },
                new Zone { ZoneId = 3, ZoneCode = "ZONE-C", ZoneName = "โซน C - เครื่องใช้สำนักงาน", CategoryId = 3 }
            );

            modelBuilder.Entity<Location>().HasData(
                new Location { LocationId = 1, BinCode = "A-01-01", ZoneId = 1, Capacity = 100 },
                new Location { LocationId = 2, BinCode = "A-01-02", ZoneId = 1, Capacity = 100 },
                new Location { LocationId = 3, BinCode = "A-02-01", ZoneId = 1, Capacity = 100 },
                new Location { LocationId = 4, BinCode = "B-01-01", ZoneId = 2, Capacity = 200 },
                new Location { LocationId = 5, BinCode = "B-01-02", ZoneId = 2, Capacity = 200 },
                new Location { LocationId = 6, BinCode = "B-02-01", ZoneId = 2, Capacity = 200 },
                new Location { LocationId = 7, BinCode = "C-01-01", ZoneId = 3, Capacity = 150 },
                new Location { LocationId = 8, BinCode = "C-01-02", ZoneId = 3, Capacity = 150 },
                new Location { LocationId = 9, BinCode = "C-02-01", ZoneId = 3, Capacity = 150 }
            );

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { UserId = 1, Username = "admin", PasswordHash = "admin123", FullName = "ผู้ดูแลระบบ", Role = UserRole.Admin },
                new AppUser { UserId = 2, Username = "warehouse1", PasswordHash = "wh123", FullName = "พนักงานคลัง 1", Role = UserRole.WarehouseStaff },
                new AppUser { UserId = 3, Username = "transport1", PasswordHash = "tp123", FullName = "พนักงานขนส่ง 1", Role = UserRole.TransportStaff },
                new AppUser { UserId = 4, Username = "inbound1", PasswordHash = "ib123", FullName = "ผู้รับงานเข้าคลัง 1", Role = UserRole.InboundStaff },
                new AppUser { UserId = 5, Username = "outbound1", PasswordHash = "ob123", FullName = "ผู้จ่ายงานออกคลัง 1", Role = UserRole.OutboundStaff },
                new AppUser { UserId = 6, Username = "manager1", PasswordHash = "mg123", FullName = "ผู้จัดการโรงงาน 1", Role = UserRole.FactoryManager }
            );

            modelBuilder.Entity<ProductMaster>().HasData(
                new ProductMaster { ProductId = 1, SKU = "ELEC-001", ProductName = "โน้ตบุ๊ค Lenovo", CategoryId = 1, Barcode = "8851234001", Width = 35, Length = 25, Height = 3, Weight = 2.1m, MinStock = 10, MaxStock = 100 },
                new ProductMaster { ProductId = 2, SKU = "ELEC-002", ProductName = "เมาส์ไร้สาย Logitech", CategoryId = 1, Barcode = "8851234002", Width = 10, Length = 6, Height = 4, Weight = 0.1m, MinStock = 50, MaxStock = 500 },
                new ProductMaster { ProductId = 3, SKU = "FOOD-001", ProductName = "น้ำดื่ม 600ml", CategoryId = 2, Barcode = "8851234003", Width = 7, Length = 7, Height = 22, Weight = 0.6m, MinStock = 100, MaxStock = 2000 },
                new ProductMaster { ProductId = 4, SKU = "OFF-001", ProductName = "กระดาษ A4 80 แกรม", CategoryId = 3, Barcode = "8851234004", Width = 21, Length = 30, Height = 5, Weight = 2.5m, MinStock = 30, MaxStock = 300 }
            );
        }
    }
}
