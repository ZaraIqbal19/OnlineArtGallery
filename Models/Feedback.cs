using Art_Gallery.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Art_Gallery.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public string message { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public Art_GalleryUser User { get; set; }
    }
}
