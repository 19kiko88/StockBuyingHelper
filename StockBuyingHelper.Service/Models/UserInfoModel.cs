using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class UserInfoModel
    {
        public string Account { get; set; }
        public string Password { get; set; }
        public string PasswordSalt { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
