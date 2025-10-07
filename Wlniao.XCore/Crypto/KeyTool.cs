using System.Text.RegularExpressions;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
namespace Wlniao.Crypto
{
    /// <summary>
    /// 密钥类型
    /// </summary>
    public enum KeyType
    {
        /// <summary>
        /// 
        /// </summary>
        Generate,
        /// <summary>
        /// 
        /// </summary>
        Pkcs8
    }

    /// <summary>
    /// 密钥工具
    /// </summary>
    public class KeyTool
    {
        byte[] _pubkey;
        byte[] _privkey;
        ICipherParameters _publicKeyParameters;
        ICipherParameters _privateKeyParameters;
        /// <summary>
        /// Hex格式公钥以04开头
        /// </summary>
        public byte[] PublicKey
        {
            get
            {
                return _pubkey;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public byte[] PrivateKey
        {
            get
            {
                return _privkey;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public ICipherParameters PublicKeyParameters
        {
            get
            {
                if (_publicKeyParameters == null)
                {
                    var x9ec = GMNamedCurves.GetByName("SM2P256V1");
                    _publicKeyParameters = new ECPublicKeyParameters(x9ec.Curve.DecodePoint(_pubkey), new ECDomainParameters(x9ec));
                }
                return _publicKeyParameters;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public ICipherParameters PrivateKeyParameters
        {
            get
            {
                if (_privateKeyParameters == null)
                {
                    _privateKeyParameters = new ECPrivateKeyParameters(new BigInteger(1, _privkey), new ECDomainParameters(GMNamedCurves.GetByName("SM2P256V1")));
                }
                return _privateKeyParameters;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public KeyTool()
        {
            GenerateKey(out _pubkey, out _privkey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="privkey"></param>
        public KeyTool(byte[] pubkey, byte[] privkey)
        {
            _pubkey = pubkey;
            _privkey = privkey;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="privkey"></param>
        /// <param name="type"></param>
        public KeyTool(string pubkey, string privkey, KeyType type)
        {
            if (type == KeyType.Generate)
            {
                _pubkey = Decode(pubkey.ToLower());
                _privkey = Decode(privkey.ToLower());
            }
            else if (type == KeyType.Pkcs8)
            {
                _pubkey = ((ECPublicKeyParameters)PublicKeyFactory.CreateKey(System.Convert.FromBase64String(pubkey))).Q.GetEncoded();
                _privkey = ((ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(System.Convert.FromBase64String(privkey))).D.ToByteArray();
            }
        }
        /// <summary>
        /// 密钥对生成
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="privkey"></param>
        public void GenerateKey(out byte[] pubkey, out byte[] privkey)
        {
            var g = new ECKeyPairGenerator();
            g.Init(new ECKeyGenerationParameters(new ECDomainParameters(GMNamedCurves.GetByName("SM2P256V1")), new SecureRandom()));
            var k = g.GenerateKeyPair();
            pubkey = ((ECPublicKeyParameters)k.Public).Q.GetEncoded(false);
            privkey = ((ECPrivateKeyParameters)k.Private).D.ToByteArray();
        }

        /// <summary>
        /// 输出Hex格式的密钥对
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="privkey"></param>
        public void OutHex(out string pubkey, out string privkey)
        {
            pubkey = Hex.ToHexString(_pubkey);
            privkey = Hex.ToHexString(_privkey);
        }


        static byte[] Decode(string key)
        {
            return Regex.IsMatch(key, "^[0-9a-f]+$", RegexOptions.IgnoreCase) ? Hex.Decode(key) : System.Convert.FromBase64String(key);
        }
    }
}
