using Microsoft.EntityFrameworkCore;
using SDOC.Domain.Entities.Dwh.Dimensions;
using SDOC.Domain.Entities.Dwh.Facts;

namespace SDOC.Persitences.Context;

public class OlapOpinionsContext : DbContext
{
    public OlapOpinionsContext(DbContextOptions<OlapOpinionsContext> options) : base(options)
    {

    }

    
    public DbSet<DimClient> Clientes => Set<DimClient>();
    public DbSet<DimProduct> Productos => Set<DimProduct>();
    public DbSet<DimClass> Clasificaciones => Set<DimClass>();
    public DbSet<DimSource> Fuentes => Set<DimSource>();
    public DbSet<DimTime> Tiempos => Set<DimTime>();
    public DbSet<DimCategory> Categorias => Set<DimCategory>();
    public DbSet<FactOpinions> Opiniones => Set<FactOpinions>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // todas las tablas están bajo olap.
        modelBuilder.HasDefaultSchema("olap");

        modelBuilder.Entity<DimClient>(e =>
        {
            e.ToTable("Dim_Cliente");
            e.HasKey(x => x.ClientSK);
        });

        modelBuilder.Entity<DimProduct>(e =>
        {
            e.ToTable("Dim_Producto");
            e.HasKey(x => x.ProductSK);
        });

        modelBuilder.Entity<DimClass>(e =>
        {
            e.ToTable("Dim_Clasificacion");
            e.HasKey(x => x.ClassSk);
        });

        modelBuilder.Entity<DimSource>(e =>
        {
            e.ToTable("Dim_Fuente");
            e.HasKey(x => x.SourceSK);
        });

        modelBuilder.Entity<DimTime>(e =>
        {
            e.ToTable("Dim_Tiempo");
            e.HasKey(x => x.TimeSK);
        });

        modelBuilder.Entity<DimCategory>(e =>
        {
            e.ToTable("Dim_Categoria");
            e.HasKey(x => x.CategorySK);
        });

        modelBuilder.Entity<FactOpinions>(e =>
        {
            e.ToTable("Fact_Opiniones");
            e.HasKey(x => x.OpinionsSK); // o el PK que tengas

            // FK -> dims (ajusta los nombres de columnas reales)
            e.HasOne<DimClient>()
                .WithMany()
                .HasForeignKey(x => x.ClientSK);

            e.HasOne<DimProduct>()
                .WithMany()
                .HasForeignKey(x => x.ProductSK);

            e.HasOne<DimSource>()
                .WithMany()
                .HasForeignKey(x => x.SourceSK);

            e.HasOne<DimClass>()
                .WithMany()
                .HasForeignKey(x => x.ClassSk);

            e.HasOne<DimTime>()
                .WithMany()
                .HasForeignKey(x => x.TimeSK);
        });

        base.OnModelCreating(modelBuilder);
    }
}
