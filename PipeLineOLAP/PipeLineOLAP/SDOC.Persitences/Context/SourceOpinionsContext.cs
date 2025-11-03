using Microsoft.EntityFrameworkCore;
using SDOC.Domain.Entities.DB;

namespace SDOC.Persitences.Context
{
    public class SourceOpinionsContext : DbContext
    {
        public SourceOpinionsContext(DbContextOptions<SourceOpinionsContext> options)
            : base(options)
        {
        }

        // 🔹 Tabla origen de las reseñas web
        public DbSet<WebReviewDB> WebReviews => Set<WebReviewDB>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tabla base de datos relacional (origen)
            modelBuilder.Entity<WebReviewDB>(entity =>
            {
                entity.ToTable("WebReviews"); // Nombre real de la tabla en tu base relacional

                entity.HasKey(e => e.Id); // PK

                // Columnas y longitudes según tu CSV o BD de ejemplo
                entity.Property(e => e.Id)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.Property(e => e.IdClient)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.Property(e => e.IdProduct)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.Property(e => e.Date)
                      .HasColumnType("date");

                entity.Property(e => e.Comment)
                      .HasMaxLength(500);

                entity.Property(e => e.Rating)
                      .HasColumnType("int");

                // Si tienes un esquema, puedes añadirlo:
                // entity.ToTable("WebReviews", schema: "dbo");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
