using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace StockBuyingHelper.Service.Implements
{
    public class RsaService: IRsaService
    {
        private readonly AppSettings.CustomizeSettings _appCustSettings;

        public RsaService(IOptions<AppSettings.CustomizeSettings> appCustSettings)
        {
            _appCustSettings = appCustSettings.Value;
        }


        public string Encrypt(string text)
        {
            var _publicKey = GetPublicKeyFromPemFile(_appCustSettings.PathSettings.RsaPublicKeyPem);
            var encryptedBytes = _publicKey.Encrypt(Encoding.UTF8.GetBytes(text), false);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string Decrypt(string encrypted)
        {
            var _privateKey = GetPrivateKeyFromPemFile(_appCustSettings.PathSettings.RsaPrivateKeyPem);
            var decryptedBytes = _privateKey.Decrypt(Convert.FromBase64String(encrypted), false);
            return Encoding.UTF8.GetString(decryptedBytes, 0, decryptedBytes.Length);
        }

        public string CreateSalt()
        {
            var arySalt = "ILoveFrenchBullDog".ToArray();
            var rnd = new Random();
            var salt_1 = new StringBuilder();
            var salt_2 = new StringBuilder();

            for (int i = 0; i < 3; i++)
            {
                salt_1.Append(arySalt[rnd.Next(0, arySalt.Length - 1)]);
                salt_2.Append(arySalt[rnd.Next(0, arySalt.Length - 1)]);
            }

            return $"{salt_1}{salt_2}";
        }

        private RSACryptoServiceProvider GetPrivateKeyFromPemFile(string filePath)
        {
            using (TextReader privateKeyTextReader = new StringReader(File.ReadAllText(filePath)))
            {
                AsymmetricCipherKeyPair readKeyPair = (AsymmetricCipherKeyPair)new PemReader(privateKeyTextReader).ReadObject();

                RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)readKeyPair.Private);
                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                csp.ImportParameters(rsaParams);
                return csp;
            }
        }

        private RSACryptoServiceProvider GetPublicKeyFromPemFile(string filePath)
        {
            using (TextReader publicKeyTextReader = new StringReader(File.ReadAllText(filePath)))
            {
                RsaKeyParameters publicKeyParam = (RsaKeyParameters)new PemReader(publicKeyTextReader).ReadObject();

                RSAParameters rsaParams = DotNetUtilities.ToRSAParameters(publicKeyParam);

                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
                csp.ImportParameters(rsaParams);
                return csp;
            }
        }
    }
}
