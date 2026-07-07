using Art_Gallery.Areas.Identity.Data;
using Art_Gallery.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Art_Gallery.Data;

public class Art_GalleryContext : IdentityDbContext<Art_GalleryUser>
{
    public Art_GalleryContext(DbContextOptions<Art_GalleryContext> options)
        : base(options)
    {
    }

    public DbSet<Order> orders { get; set; }
    public DbSet<AuctionDetails> auctionDetails { get; set; }
    public DbSet<Category> categories { get; set; }
    public DbSet<Contact> contacts { get; set; }
    public DbSet<Feedback> feedbacks { get; set; }
    public DbSet<Payment> payments { get; set; }
    public DbSet<Payment_Details> paymentDetails { get; set; }
    public DbSet<Product> products { get; set; }
    public DbSet<ProductReview> productReviews { get; set; }
    public DbSet<SubCategory> subCategories { get; set; }
    public DbSet<Wishlist> wishlist { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
        foreach (var foreignKey in builder.Model.GetEntityTypes()
                    .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }

    }
}
