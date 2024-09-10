using System.ComponentModel.DataAnnotations;

namespace SakuraSushi_API.Request
{
    public class CartRequire
    {
        [Required(ErrorMessage = "itemId Required")]
        public string itemId { get; set; }
        [Required(ErrorMessage = "quantity Required")]
        public int quantity { get; set; }
    }
}
