using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Models
{
    public class AppSettings
    {        
        public const string _ConnectionStrings = "ConnectionStrings";
        public const string _JwtSettings = "JwtSettings";

        public class ConnectionStrings
        {
            public string SBHConnection { get; set; }
        }

        public class JwtSettings
        { 
            public string ValidIssuer { get; set; }
            public string ValidAudience { get; set; }
            public string Key { get; set; }
        }

        public class CustomizeSettings
        {
            public string OperationSystem { get; set; } = string.Empty;
            public PathSettings? PathSettings { get; set; }
            public List<string>? List0050 { get; set; }
        }
    }

    public class PathSettings
    {
        public string RsaPublicKeyPem { get; set; } = string.Empty;
        public string RsaPrivateKeyPem { get; set; } = string.Empty;
        public string HighLow52Data { get; set; } = string.Empty;        
    }
}
