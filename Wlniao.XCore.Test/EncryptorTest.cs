using NUnit.Framework;

namespace Wlniao.XCore.Test
{
    public class EncryptorTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SM2EncryptAndDecrypt()
        {
            var data = "hello world!";
            var pubkey = "041e9c284e27529094ea9a17acf612215ec1f97b4a03d269a1f3da228ff07f49a782218c3cfb7ae874238fdf538be88c40a0e8d3ee750f453bc8c43f126f1e99eb";
            var privkey = "0bc7da110f90bc3a37b407ffc5c7cb8a7f7d9ebd9d4d9bda1aa36f71df04244d";
            var encryText = Wlniao.Encryptor.SM2EncryptByPublicKey(data, pubkey);
            var plainText = Wlniao.Encryptor.SM2DecryptByPrivateKey(encryText, privkey);
            // assert
            Assert.AreEqual(data, plainText);
        }


        [Test]
        public void SM2Decrypt()
        {
            var data = "040984af5d3d9cb71b39a061f16b9f0c19e5b692cd2ab7b943f3f7cc43fba1967ce4dc87d1f5af5f13f1f92a79629873a479d5173f8d5a98544f627c1d6daccdc3e18b16a761801df2800523dd4c2801c6a81cb16d253fae9deb40b2705b40fe5cdf90aaf10b1f44cec233e86e";
            var pubkey = "308201333081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d541230201010342000421ad50bb500737e286d64f7b0e7413f8b69da68c2cae6995be86b1b0261d69fc847aec7c99aa3542fd2c9e2896957f0e46e9fdfcc6e31b51c090814df58bad38";
            var privkey = "3082024b0201003081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d5412302010104820155308201510201010420eebf1ae134e71ce7bd06979b3cc273ed5eb0677030c08ffccf191af5c93220aca081e33081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d54123020101a1440342000421ad50bb500737e286d64f7b0e7413f8b69da68c2cae6995be86b1b0261d69fc847aec7c99aa3542fd2c9e2896957f0e46e9fdfcc6e31b51c090814df58bad38";
            var handle = new Wlniao.Crypto.SM2(pubkey, privkey, Crypto.KeyType.Pkcs8);
            var plainBytes = handle.Decrypt(data);
            var result = Encoding.UTF8.GetString(plainBytes);
            // assert
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void SM2Encrypt()
        {
            var data = "hello world!";
            var pubkey = "308201333081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d541230201010342000421ad50bb500737e286d64f7b0e7413f8b69da68c2cae6995be86b1b0261d69fc847aec7c99aa3542fd2c9e2896957f0e46e9fdfcc6e31b51c090814df58bad38";
            var privkey = "3082024b0201003081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d5412302010104820155308201510201010420eebf1ae134e71ce7bd06979b3cc273ed5eb0677030c08ffccf191af5c93220aca081e33081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d54123020101a1440342000421ad50bb500737e286d64f7b0e7413f8b69da68c2cae6995be86b1b0261d69fc847aec7c99aa3542fd2c9e2896957f0e46e9fdfcc6e31b51c090814df58bad38";
            var handle = new Wlniao.Crypto.SM2(pubkey, privkey, Crypto.KeyType.Pkcs8);
            var encryBytes = handle.Encrypt(data);
            var result = Wlniao.Convert.BytesToHexString(encryBytes);
            // assert
            Assert.IsNotEmpty(result);
        }


        [Test]
        public void SM2Sign()
        {
            var data = "hello world!";
            var pubkey = "308201333081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d541230201010342000421ad50bb500737e286d64f7b0e7413f8b69da68c2cae6995be86b1b0261d69fc847aec7c99aa3542fd2c9e2896957f0e46e9fdfcc6e31b51c090814df58bad38";
            var privkey = "3082024b0201003081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d5412302010104820155308201510201010420eebf1ae134e71ce7bd06979b3cc273ed5eb0677030c08ffccf191af5c93220aca081e33081e0020101302c06072a8648ce3d0101022100fffffffeffffffffffffffffffffffffffffffff00000000ffffffffffffffff30440420fffffffeffffffffffffffffffffffffffffffff00000000fffffffffffffffc042028e9fa9e9d9f5e344d5a9e4bcf6509a7f39789f515ab8f92ddbcbd414d940e9304410432c4ae2c1f1981195f9904466a39c9948fe30bbff2660be1715a4589334c74c7bc3736a2f4f6779c59bdcee36b692153d0a9877cc62a474002df32e52139f0a0022100fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d54123020101a1440342000421ad50bb500737e286d64f7b0e7413f8b69da68c2cae6995be86b1b0261d69fc847aec7c99aa3542fd2c9e2896957f0e46e9fdfcc6e31b51c090814df58bad38";
            var handle = new Wlniao.Crypto.SM2(pubkey, privkey, Crypto.KeyType.Pkcs8, Crypto.SM2Mode.C1C3C2);
            var plainBytes = Encoding.UTF8.GetBytes(data);
            var signBytes = handle.Sign(plainBytes);
            var result = Wlniao.Convert.BytesToHexString(signBytes);
            var verifyBytes = Wlniao.Crypto.Helper.Decode(result);
            var verifySingResult = handle.VerifySign(plainBytes, verifyBytes, null);

            // assert
            Assert.IsTrue(verifySingResult);
        }
        [Test]
        public void SM2VerifySign()
        {
            var pubkey = Wlniao.Crypto.Helper.Decode("BJ4zOEUv3GZ0H/pPAz/z6TpLMb9005h1VNiVRmVSad8otoeo6otBLSqQ+qlDZdCT3f/kLP2JNconmoUf01ENbqk=");
            var privkey = Wlniao.Crypto.Helper.Decode("ALGyqdbg0mn2pmqFjKcFMgUUtbcQoU1bqOrI1CAx69UZ");
            var handle = new Wlniao.Crypto.SM2(pubkey, privkey, Crypto.SM2Mode.C1C3C2);
            var plainBytes = Encoding.UTF8.GetBytes("abcdefg");
            var signData = handle.Sign(plainBytes);
            var signStr = System.Convert.ToBase64String(signData);
            var signBytes = Wlniao.Crypto.Helper.Decode(signStr);
            //signBytes = Wlniao.Crypto.SM2.RsPlainByteArrayToAsn1(signBytes);
            var verifySingResult = handle.VerifySign(plainBytes, signBytes, null);

            // assert
            Assert.IsTrue(verifySingResult);
        }
        [Test]
        public void SM3Encryptor()
        {
            var value = Wlniao.Encryptor.SM3Encrypt("wlniao studio");
            Assert.GreaterOrEqual(value, "dce717c7430e26a0f41d33840bbe6baa64e3d915d4d54d1bf8f7a07b093dfec0");
        }


        [Test]
        public void SM4EncryptECBToHex()
        {
            var value = Wlniao.Encryptor.SM4EncryptECBToHex("wlniao studio", "a8c9d69e29080f1d");
            Assert.GreaterOrEqual(value, "b01a5eed2708c24a941053f82af23882");
        }

        [Test]
        public void SM4EncryptCBCToBase64()
        {
            var value = Wlniao.Encryptor.SM4EncryptCBCToBase64("wlniao studio", "a8c9d69e29080f1d", "39ad0d2989a25606");
            Assert.GreaterOrEqual(value, "3gS+A9JgJpyXghus0UapmQ==");
        }
        [Test]
        public void SM4DecryptECBFromBase64()
        {
            var value = Wlniao.Encryptor.SM4DecryptECBFromBase64("sBpe7ScIwkqUEFP4KvI4gg==", "a8c9d69e29080f1d");
            Assert.GreaterOrEqual(value, "wlniao studio");
        }

        [Test]
        public void SM4DecryptCBCFromHex()
        {
            var value = Wlniao.Encryptor.SM4DecryptCBCFromHex("de04be03d260269c97821bacd146a999", "a8c9d69e29080f1d", "39ad0d2989a25606");
            Assert.GreaterOrEqual(value, "wlniao studio");
        }
    }
}
