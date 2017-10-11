using Microsoft.EntityFrameworkCore;

namespace WorldCollector.Taobao.Models
{
    public class TaobaoCollectorDbContext : DbContext
    {
        public DbSet<TaobaoItem> TaobaoItems { get; set; }

        public TaobaoCollectorDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
                "server=localhost;userid=root;pwd=root;database=taobao_image_collector;sslmode=none;");
        }

        public TaobaoCollectorDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaobaoItem>(t =>
            {
                t.HasIndex(a => new {Id = a.ItemId, a.LastCheckDt});
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
