using Art_Gallery.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Art_Gallery.Models
{
    public class Payment_Details
    {
        public int Id { get; set; }

        public int CardNumber { get; set; }
        public string CardTitle { get; set; }
        public int CVVCode { get; set; }
        public DateOnly DateofExpiry { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public Art_GalleryUser User { get; set; }

    }
}
