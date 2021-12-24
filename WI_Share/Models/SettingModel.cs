using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WI_Share.Models
{
    public class SettingModel
    {
        [Required]
        [DisplayName("SSID")]
        public string ssid { get; set; }
        [Required]
        [DisplayName("Password")]
        public string password { get; set; }
        [Required]
        [DisplayName("IP Address")]
        public string ipAdress { get; set; }
    }
}
