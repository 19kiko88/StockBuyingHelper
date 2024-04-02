using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Interfaces
{
    public interface IRsaService
    {
        public string Encrypt(string text);
        public string Decrypt(string encrypted);
        public string CreateSalt();
    }
}
