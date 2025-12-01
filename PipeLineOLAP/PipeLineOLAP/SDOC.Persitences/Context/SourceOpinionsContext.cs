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
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
