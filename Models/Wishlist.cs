using Art_Gallery.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Art_Gallery.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public Art_GalleryUser User { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }
}
