using Microsoft.EntityFrameworkCore;
using TodoDAL.Entities;

namespace TodoDAL.Data
{
    public sealed class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
    {
        public DbSet<TodoItem>? TodoItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>(entity =>
            {
                entity.ToTable("TodoItems");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.IsComplete);

                entity.Property(e => e.FileName)  
                    .HasMaxLength(255);

                entity.Property(e => e.OcrText)
                    .HasMaxLength(-1);

            });

            base.OnModelCreating(modelBuilder);
        }
    }
}