using System.ComponentModel.DataAnnotations.Schema;

namespace Art_Gallery.Models
{
    public class SubCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public String SubCategoryimage { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]

        public Category category { get; set; }
    }
}
