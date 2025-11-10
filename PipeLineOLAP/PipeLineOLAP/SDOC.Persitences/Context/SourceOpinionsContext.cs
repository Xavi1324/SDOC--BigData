using Microsoft.EntityFrameworkCore;
using SDOC.Domain.Entities.Api;
using SDOC.Domain.Entities.DB;

namespace SDOC.Persitences.Context
{
    public class SourceOpinionsContext : DbContext
    {
        public SourceOpinionsContext(DbContextOptions<SourceOpinionsContext> options)  : base(options)
        {

        }

        
        public DbSet<WebReviewDB> WebReviews { get; set; }
        public DbSet<SocialCommetsApi> SocialCommets { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<WebReviewDB>(entity =>
            {
                entity.HasNoKey(); // Es una vista, no tabla
                entity.ToView("vw_WebReviews", "dw");

                entity.Property(e => e.OpinionId).HasColumnName("OpinionId");
                entity.Property(e => e.ProductId).HasColumnName("ProductId");
                entity.Property(e => e.ClientId).HasColumnName("ClientId");
                entity.Property(e => e.FuenteId).HasColumnName("FuenteId");
                entity.Property(e => e.TimeId).HasColumnName("TimeId");
                entity.Property(e => e.Comment).HasColumnName("Comment");
                entity.Property(e => e.ClassId).HasColumnName("ClassId");
            });

            //modelBuilder.Entity<SocialCommetsApi>(entity =>
            //{
            //    entity.HasNoKey(); // Es una vista, no una tabla con PK real
            //    entity.ToView("vw_SocialCommetsApi", "dw");

            //    entity.Property(e => e.OpinionId).HasColumnName("Id");               
            //    entity.Property(e => e.IdClient).HasColumnName("IdClient");
            //    entity.Property(e => e.IdProduct).HasColumnName("IdProduct");
            //    entity.Property(e => e.Source).HasColumnName("Source");
            //    entity.Property(e => e.Comment).HasColumnName("Comment");
            //});

            base.OnModelCreating(modelBuilder);
        }
    }
}
