using System.ComponentModel.DataAnnotations.Schema;

namespace Art_Gallery.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string ModeofPayment { get; set; }
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; }
    }
}
