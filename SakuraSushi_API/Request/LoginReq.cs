using System.ComponentModel.DataAnnotations;

namespace SakuraSushi_API.Request
{
    public class LoginReq
    {
        [Required(ErrorMessage = "Username Required")]
        public string username { get; set; }
        [Required(ErrorMessage = "Password Required")]
        public string password { get; set; }
    }
}
