using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace demodemo.Models;

public partial class LopuxDbContext : DbContext
{
    public LopuxDbContext()
    {
    }

    public LopuxDbContext(DbContextOptions<LopuxDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<AgentPriorityHistory> AgentPriorityHistories { get; set; }

    public virtual DbSet<AgentType> AgentTypes { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MaterialCountHistory> MaterialCountHistories { get; set; }

    public virtual DbSet<MaterialType> MaterialTypes { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCostHistory> ProductCostHistories { get; set; }

    public virtual DbSet<ProductMaterial> ProductMaterials { get; set; }

    public virtual DbSet<ProductSale> ProductSales { get; set; }

    public virtual DbSet<ProductType> ProductTypes { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;port=5433;Database=LopuxDb;username=Lopux;password=Lopux");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.ToTable("Agent");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.AgentTypeId).HasColumnName("AgentTypeID");
            entity.Property(e => e.DirectorName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Inn)
                .HasMaxLength(12)
                .HasColumnName("INN");
            entity.Property(e => e.Kpp)
                .HasMaxLength(9)
                .HasColumnName("KPP");
            entity.Property(e => e.Logo).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.AgentType).WithMany(p => p.Agents)
                .HasForeignKey(d => d.AgentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Agent_AgentType");
        });

        modelBuilder.Entity<AgentPriorityHistory>(entity =>
        {
            entity.ToTable("AgentPriorityHistory");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.AgentId).HasColumnName("AgentID");
            entity.Property(e => e.ChangeDate).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Agent).WithMany(p => p.AgentPriorityHistories)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgentPriorityHistory_Agent");
        });

        modelBuilder.Entity<AgentType>(entity =>
        {
            entity.ToTable("AgentType");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.Image).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(50);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.ToTable("Material");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.Cost).HasPrecision(10, 2);
            entity.Property(e => e.Image).HasMaxLength(100);
            entity.Property(e => e.MaterialTypeId).HasColumnName("MaterialTypeID");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(10);

            entity.HasOne(d => d.MaterialType).WithMany(p => p.Materials)
                .HasForeignKey(d => d.MaterialTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Material_MaterialType");

            entity.HasMany(d => d.Suppliers).WithMany(p => p.Materials)
                .UsingEntity<Dictionary<string, object>>(
                    "MaterialSupplier",
                    r => r.HasOne<Supplier>().WithMany()
                        .HasForeignKey("SupplierId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MaterialSupplier_Supplier"),
                    l => l.HasOne<Material>().WithMany()
                        .HasForeignKey("MaterialId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MaterialSupplier_Material"),
                    j =>
                    {
                        j.HasKey("MaterialId", "SupplierId");
                        j.ToTable("MaterialSupplier");
                        j.IndexerProperty<int>("MaterialId").HasColumnName("MaterialID");
                        j.IndexerProperty<int>("SupplierId").HasColumnName("SupplierID");
                    });
        });

        modelBuilder.Entity<MaterialCountHistory>(entity =>
        {
            entity.ToTable("MaterialCountHistory");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.ChangeDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialCountHistories)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MaterialCountHistory_Material");
        });

        modelBuilder.Entity<MaterialType>(entity =>
        {
            entity.ToTable("MaterialType");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.Title).HasMaxLength(50);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.ArticleNumber).HasMaxLength(10);
            entity.Property(e => e.Image).HasMaxLength(100);
            entity.Property(e => e.MinCostForAgent).HasPrecision(10, 2);
            entity.Property(e => e.ProductTypeId).HasColumnName("ProductTypeID");
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.ProductType).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductTypeId)
                .HasConstraintName("FK_Product_ProductType");
        });

        modelBuilder.Entity<ProductCostHistory>(entity =>
        {
            entity.ToTable("ProductCostHistory");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.ChangeDate).HasColumnType("timestamp without time zone");
            entity.Property(e => e.CostValue).HasPrecision(10, 2);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductCostHistories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductCostHistory_Product");
        });

        modelBuilder.Entity<ProductMaterial>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.MaterialId });

            entity.ToTable("ProductMaterial");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.MaterialId).HasColumnName("MaterialID");

            entity.HasOne(d => d.Material).WithMany(p => p.ProductMaterials)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductMaterial_Material");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductMaterials)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductMaterial_Product");
        });

        modelBuilder.Entity<ProductSale>(entity =>
        {
            entity.ToTable("ProductSale");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.AgentId).HasColumnName("AgentID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Agent).WithMany(p => p.ProductSales)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSale_Agent");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSales)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSale_Product");
        });

        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.ToTable("ProductType");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.Title).HasMaxLength(50);
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("Shop");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.AgentId).HasColumnName("AgentID");
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.Agent).WithMany(p => p.Shops)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shop_Agent");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Supplier");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("ID");
            entity.Property(e => e.Inn)
                .HasMaxLength(12)
                .HasColumnName("INN");
            entity.Property(e => e.SupplierType).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(150);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
