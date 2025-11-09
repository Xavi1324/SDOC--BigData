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
                entity.ToTable("Opinion", "dw");

                entity.HasKey(e => e.OpinionId);

                entity.Property(e => e.OpinionId).HasColumnName("Opinion_Id");
                entity.Property(e => e.ProductId).HasColumnName("Product_Id");
                entity.Property(e => e.ClientId).HasColumnName("Client_Id");
                entity.Property(e => e.FuenteId).HasColumnName("Fuente_Id");
                entity.Property(e => e.TimeId).HasColumnName("Time_Id");
                entity.Property(e => e.Comment).HasColumnName("Comment");
                entity.Property(e => e.ClassId).HasColumnName("Class_Id");
                entity.Property(e => e.HashUnique).HasColumnName("HashUnique");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
