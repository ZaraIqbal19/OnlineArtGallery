using Art_Gallery.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Art_Gallery.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string Image1 { get; set; }
        public string Image2 { get; set; }
        public string Image3 { get; set; }
        public float price { get; set; }
        public int quantity { get; set; }
        public int SubCategoryId {  get; set; }
        [ForeignKey("SubCategoryId")]

        public SubCategory SubCategory { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public Art_GalleryUser User { get; set; }
        public string Status { get; set; }
        public string AvailableForBid {  get; set; }

        public DateOnly BidStartDate { get; set; }

        public DateOnly BidEndDate { get; set; }

        public float BidPrice { get; set; }
    }
}
