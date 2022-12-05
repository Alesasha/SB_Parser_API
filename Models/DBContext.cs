using System;
using System.Collections.Generic;
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
        public PISBContext(DbContextOptions<PISBContext> options) : base(options) {}
        //protected readonly IConfiguration Configuration=null!;
        public PISBContext() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Get_DB_ConnectionString("PISB"),//"Name=PISB",//"Name=PISB", //Configuration.GetConnectionString("PISB")!
            options => options.EnableRetryOnFailure());
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(System.Console.WriteLine, LogLevel.Information);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Retailer>(entity =>
            {
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
                entity.Property(e => e.slug).HasMaxLength(50);
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.Property(e => e.id).ValueGeneratedNever();
                entity.Property(e => e.building).HasMaxLength(100);
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
            });

            //OnModelCreatingPartial(modelBuilder);
        }
        //partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
