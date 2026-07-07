using Art_Gallery.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Art_Gallery.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public Art_GalleryUser User { get; set; }
    }
}
