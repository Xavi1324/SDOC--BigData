using Microsoft.EntityFrameworkCore;
using SDOC.Domain.Entities.Dwh.Dimensions;
using SDOC.Domain.Entities.Dwh.Facts;

namespace SDOC.Persitences.Context;

public class OlapOpinionsContext : DbContext
{
    public OlapOpinionsContext(DbContextOptions<OlapOpinionsContext> options) : base(options)
    {
        
    }

    public DbSet<DimClient> DimClients => Set<DimClient>();
    public DbSet<DimProduct> DimProducts => Set<DimProduct>();
    public DbSet<DimClass> DimClasses => Set<DimClass>();
    public DbSet<DimSource> DimSources => Set<DimSource>();
    public DbSet<DimTime> DimTimes => Set<DimTime>();
    public DbSet<DimCategory> DimCategories => Set<DimCategory>();
    public DbSet<FactOpinions> Opiniones => Set<FactOpinions>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("olap");

        modelBuilder.Entity<DimClient>(e =>
        {
            e.ToTable("Dim_Cliente");
            e.HasKey(x => x.ClientSK);

            e.Property(x => x.ClientSK).HasColumnName("Client_SK");
            e.Property(x => x.ClientName).HasColumnName("ClientName");
            e.Property(x => x.LastName).HasColumnName("LastName");
            e.Property(x => x.Email).HasColumnName("Email");
            e.Property(x => x.Country).HasColumnName("PaisNombre");
        });

        modelBuilder.Entity<DimCategory>(e =>
        {
            e.ToTable("Dim_Categoria");
            e.HasKey(x => x.CategorySK);

            e.Property(x => x.CategorySK).HasColumnName("Categoria_SK");
            e.Property(x => x.CategoryName).HasColumnName("Nombre");
        });

        modelBuilder.Entity<DimProduct>(e =>
        {
            e.ToTable("Dim_Producto");
            e.HasKey(x => x.ProductSK);

            e.Property(x => x.ProductSK).HasColumnName("Product_SK");
            e.Property(x => x.ProductName).HasColumnName("ProductName");
            e.Property(x => x.CategorySK).HasColumnName("Categoria_SK");

            e.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategorySK);
        });

        modelBuilder.Entity<DimSource>(e =>
        {
            e.ToTable("Dim_Fuente");
            e.HasKey(x => x.SourceSK);

            e.Property(x => x.SourceSK).HasColumnName("Fuente_SK");
            e.Property(x => x.SourceName).HasColumnName("NombreFuente");
            e.Property(x => x.SourceType).HasColumnName("TipoFuenteDesc");
        });

        modelBuilder.Entity<DimClass>(e =>
        {
            e.ToTable("Dim_Clasificacion");
            e.HasKey(x => x.ClassSk);

            e.Property(x => x.ClassSk).HasColumnName("Class_SK");
            e.Property(x => x.ClassCode).HasColumnName("Class_Code");
            e.Property(x => x.ClassName).HasColumnName("Class_Nombre");
        });

        modelBuilder.Entity<DimTime>(e =>
        {
            e.ToTable("Dim_Tiempo");
            e.HasKey(x => x.TimeSK);

            e.Property(x => x.TimeSK).HasColumnName("Time_SK");
            e.Property(x => x.Date).HasColumnName("Date");
            e.Property(x => x.Year).HasColumnName("Year");
            e.Property(x => x.Month).HasColumnName("Month");
            e.Property(x => x.MonthName).HasColumnName("MonthName");
            e.Property(x => x.Day).HasColumnName("Day");
            e.Property(x => x.DayName).HasColumnName("DayName");
            e.Property(x => x.Quarter).HasColumnName("Quarter");
            e.Property(x => x.WeekOfYear).HasColumnName("WeekOfYear");
        });

        // 🔹 Deja FactOpinions como keyless por ahora
        modelBuilder.Entity<FactOpinions>(e =>
        {
            e.ToTable("Fact_Opiniones");
            e.HasKey(f => f.OpinionsSK);
        });

        base.OnModelCreating(modelBuilder);
    }
            
    
}
