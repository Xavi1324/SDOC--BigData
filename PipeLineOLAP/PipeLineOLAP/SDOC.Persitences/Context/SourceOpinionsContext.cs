using Microsoft.EntityFrameworkCore;
using SDOC.Domain.Entities.DB;

namespace SDOC.Persitences.Context
{
    public class SourceOpinionsContext : DbContext
    {
        public SourceOpinionsContext(DbContextOptions<SourceOpinionsContext> options)  : base(options)
        {

        }

        
        public DbSet<WebReviewDB> WebReviews { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<WebReviewDB>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_WebReviews", "dw");

                entity.Property(e => e.OpinionId).HasColumnName("OpinionId");
                entity.Property(e => e.ProductId).HasColumnName("ProductId");
                entity.Property(e => e.ClientId).HasColumnName("ClientId");
                entity.Property(e => e.FuenteId).HasColumnName("FuenteId");
                entity.Property(e => e.TimeId).HasColumnName("TimeId");
                entity.Property(e => e.Comment).HasColumnName("Comment");
                entity.Property(e => e.ClassId).HasColumnName("ClassId");
                entity.Property(e => e.ProductName).HasColumnName("ProductName");
                entity.Property(e => e.CategoryId).HasColumnName("CategoryId");
                entity.Property(e => e.CategoryName).HasColumnName("CategoryName");

                entity.Property(e => e.ClientName).HasColumnName("ClientName");
                entity.Property(e => e.LastName).HasColumnName("LastName");
                entity.Property(e => e.Email).HasColumnName("Email");

                entity.Property(e => e.FuenteNombre).HasColumnName("FuenteNombre");
                entity.Property(e => e.TipoFuenteDesc).HasColumnName("TipoFuenteDesc");

                entity.Property(e => e.Fecha).HasColumnName("Fecha");

                entity.Property(e => e.ClassCode).HasColumnName("ClassCode");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
