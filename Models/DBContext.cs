using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using static SB_Parser_API.MicroServices.Utils;

namespace SB_Parser_API.Models
{

    //_________________________ EF Core______________________________________//
    //To protect potentially sensitive information in your connection string, you should move it out of source code.
    //        You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration
    //        see https://go.microsoft.com/fwlink/?linkid=2131148. 
    //        For more guidance on storing connection strings,
    //        see http://go.microsoft.com/fwlink/?LinkId=723263.

    public class PISBContext : DbContext
    {
        public DbSet<Retailer> Retailers => Set<Retailer>();
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Product_SB_V2> Products => Set<Product_SB_V2>();
        public DbSet<Property_Product_DB> ProductProperties => Set<Property_Product_DB>();
        public DbSet<SB_Image> Images => Set<SB_Image>();
        public DbSet<SB_Barcode> Barcodes => Set<SB_Barcode>();
        public DbSet<SB_image_product> ImProds => Set<SB_image_product>();
        public DbSet<SB_barcode_product> BarProds => Set<SB_barcode_product>();
        public DbSet<Price_SB_V2> Prices => Set<Price_SB_V2>();
        public DbSet<Price_SB_V2_Archive> Prices_Archive => Set<Price_SB_V2_Archive>();
        public DbSet<ZC_User> Users => Set<ZC_User>();
        public DbSet<Query_ZC> Queries => Set<Query_ZC>();
        public DbSet<Query_Type_ZC> QueryTypes => Set<Query_Type_ZC>();
        public PISBContext(DbContextOptions<PISBContext> options) : base(options) {}
        //protected readonly IConfiguration Configuration=null!;
        public PISBContext() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Get_DB_ConnectionString("PISB"),//"Name=PISB",//"Name=PISB", //Configuration.GetConnectionString("PISB")!
            options => options.EnableRetryOnFailure());
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(System.Console.WriteLine, LogLevel.Error);
            //this.Database.SetCommandTimeout(Get_DB_CommandTimeOut());
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Retailer>(entity =>
            {
                entity.ToTable("SB_Retailers");
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.contract_type).HasMaxLength(30);
                entity.Property(e => e.description).HasMaxLength(255);
                entity.Property(e => e.inn).HasMaxLength(20);
                entity.Property(e => e.key).HasMaxLength(20);
                entity.Property(e => e.key_account_manager_fullname).HasMaxLength(50);
                entity.Property(e => e.legal_address).HasMaxLength(300);
                entity.Property(e => e.legal_name).HasMaxLength(200);
                entity.Property(e => e.logo_image).HasMaxLength(1024);
                entity.Property(e => e.master_retailer_name).HasMaxLength(30);
                entity.Property(e => e.mini_logo_image).HasMaxLength(1024);
                entity.Property(e => e.name).HasMaxLength(100);
                entity.Property(e => e.phone).HasMaxLength(20);
                entity.Property(e => e.secondary_color).HasMaxLength(20);
                entity.Property(e => e.seo_category).HasMaxLength(30);
                entity.Property(e => e.short_name).HasMaxLength(100);
                entity.Property(e => e.side_image).HasMaxLength(1024);
                entity.Property(e => e.slug).HasMaxLength(100);
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.ToTable("SB_Stores");
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.building).HasMaxLength(200);
                entity.Property(e => e.city).HasMaxLength(200);
                entity.Property(e => e.city_name).HasMaxLength(100);
                entity.Property(e => e.city_slug).HasMaxLength(100);
                entity.Property(e => e.closing_time).HasMaxLength(20);
                entity.Property(e => e.full_address).HasMaxLength(500);
                entity.Property(e => e.location_phone).HasMaxLength(50);
                entity.Property(e => e.name).HasMaxLength(500);
                entity.Property(e => e.opening_time).HasMaxLength(20);
                entity.Property(e => e.operational_zone_name).HasMaxLength(100);
                entity.Property(e => e.orders_api_integration_type).HasMaxLength(50);
                entity.Property(e => e.pharmacy_license).HasMaxLength(200);
                entity.Property(e => e.phone).HasMaxLength(70);
                entity.Property(e => e.retailer_store_id).HasMaxLength(100);
                entity.Property(e => e.street).HasMaxLength(250);
                entity.Property(e => e.time_zone).HasMaxLength(50);
                entity.Property(e => e.uuid).HasMaxLength(50);
            });
            
            modelBuilder.Entity<Product_SB_V2>(entity =>
            {
                entity.ToTable("SB_Products");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.price_type).HasMaxLength(50);
                entity.Property(e => e.volume_type).HasMaxLength(20);
                entity.Property(e => e.human_volume).HasMaxLength(30);
                entity.Property(e => e.name).HasMaxLength(300);
            });

            modelBuilder.Entity<Property_Product_DB>(entity =>
            {
                entity.ToTable("SB_Product_Properties");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.name).HasMaxLength(100);
                entity.Property(e => e.presentation).HasMaxLength(100);
                //entity.Property(e => e.value).HasMaxLength(4000);
            });

            modelBuilder.Entity<SB_Image>(entity =>
            {
                entity.ToTable("SB_Images");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.url).HasMaxLength(500);
            });
            modelBuilder.Entity<SB_Barcode>(entity =>
            {
                entity.ToTable("SB_Barcodes");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.barcode).HasMaxLength(100);
            });
            modelBuilder.Entity<SB_image_product>(entity =>
            {
                entity.ToTable("SB_image_product");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
            });
            modelBuilder.Entity<SB_barcode_product>(entity =>
            {
                entity.ToTable("SB_barcode_product");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
            });
            modelBuilder.Entity<Price_SB_V2>(entity =>
            {
                entity.ToTable("SB_Prices");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
            });
            modelBuilder.Entity<Price_SB_V2_Archive>(entity =>
            {
                entity.ToTable("SB_Prices_Archive");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
            });
            modelBuilder.Entity<ZC_User>(entity =>
            {
                entity.ToTable("ZC_Users");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.info).HasMaxLength(500);
            });
            modelBuilder.Entity<Query_ZC>(entity =>
            {
                entity.ToTable("ZC_Queries");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.query).HasMaxLength(250);
            });
            modelBuilder.Entity<Query_Type_ZC>(entity =>
            {
                entity.ToTable("ZC_Query_Types");
                entity.HasKey(e => e.id);
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.type).HasMaxLength(50);
            });
            this.Database.SetCommandTimeout(Get_DB_CommandTimeOut());
        }
    }
    public class PIPContext : DbContext
    {
        public virtual DbSet<DeadProxy> DeadProxies { get; set; } = null!;
        public virtual DbSet<IG_Proxy_Token> IG_Proxy_Tokens { get; set; } = null!;
        public virtual DbSet<IG_Proxy_Token_Archive> IG_Proxy_Token_Archives { get; set; } = null!;
        public virtual DbSet<Proxy> Proxies { get; set; } = null!;
        public virtual DbSet<PI_Parameter> PI_Parameters { get; set; } = null!;
        public virtual DbSet<Proxy_to_Domain> Proxy_to_Domain { get; set; } = null!;
        public PIPContext(DbContextOptions<PIPContext> options) : base(options) { }
        public PIPContext() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //this.Database.SetCommandTimeout(Get_DB_CommandTimeOut());
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Get_DB_ConnectionString("PIP"),
                options => options.EnableRetryOnFailure());
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.LogTo(System.Console.WriteLine, LogLevel.Error);//.Information
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeadProxy>(entity =>
            {
                //entity.HasNoKey();
                entity.ToTable("DeadProxy");
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.defence).HasMaxLength(255);
                entity.Property(e => e.ip).HasMaxLength(255);
                entity.Property(e => e.location).HasMaxLength(255);
                entity.Property(e => e.login).HasMaxLength(50);
                entity.Property(e => e.pass).HasMaxLength(50);
                entity.Property(e => e.port).HasMaxLength(50);
                entity.Property(e => e.protocol).HasMaxLength(255);
            });

            modelBuilder.Entity<IG_Proxy_Token>(entity =>
            {
                //entity.HasNoKey();
                entity.HasKey(e => new { e.proxy, e.xUserToken });
                entity.ToTable("IG_Proxy_Token");
                entity.Property(e => e.afUserId).HasMaxLength(100);
                entity.Property(e => e.proxy).HasMaxLength(255);
                entity.Property(e => e.xUserId).HasMaxLength(50);
                entity.Property(e => e.xUserToken).HasMaxLength(50);
            });

            modelBuilder.Entity<IG_Proxy_Token_Archive>(entity =>
            {
                entity.HasNoKey();
                entity.ToTable("IG_Proxy_Token_Archive");
                entity.Property(e => e.afUserId).HasMaxLength(100);
                entity.Property(e => e.proxy).HasMaxLength(255);
                entity.Property(e => e.xUserId).HasMaxLength(50);
                entity.Property(e => e.xUserToken).HasMaxLength(50);
            });

            modelBuilder.Entity<Proxy>(entity =>
            {
                entity.ToTable("Proxy");
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.defence).HasMaxLength(255);
                entity.Property(e => e.ip).HasMaxLength(255);
                entity.Property(e => e.location).HasMaxLength(255);
                entity.Property(e => e.login).HasMaxLength(50);
                entity.Property(e => e.pass).HasMaxLength(50);
                entity.Property(e => e.port).HasMaxLength(50);
                entity.Property(e => e.protocol).HasMaxLength(255);
            });

            modelBuilder.Entity<PI_Parameter>(entity =>
            {
                entity.ToTable("PI_Parameters");
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.note).HasMaxLength(500);
                entity.Property(e => e.valueS).HasMaxLength(4000);
            });

            modelBuilder.Entity<Proxy_to_Domain>(entity =>
            {
                //entity.HasNoKey();
                entity.ToTable("Proxy_to_Domain");
                entity.HasKey(e => new { e.ip, e.port, e.protocol, e.domain});
                entity.Property(e => e.ip).HasMaxLength(255);
                entity.Property(e => e.port).HasMaxLength(50);
                entity.Property(e => e.protocol).HasMaxLength(255);
                entity.Property(e => e.domain).HasMaxLength(255);
                entity.Property(e => e.parametrs).HasMaxLength(4000);
                //entity.Ignore(e => e.getParametr);
                //entity.Ignore(e => e.setParametr);
            });
            this.Database.SetCommandTimeout(Get_DB_CommandTimeOut());

            //OnModelCreatingPartial(modelBuilder);
        }
        //partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
