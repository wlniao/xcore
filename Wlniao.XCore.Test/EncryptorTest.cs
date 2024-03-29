﻿using NUnit.Framework;

namespace Wlniao.XCore.Test
{
    public class EncryptorTest
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void SM2Decrypt()
        {
            var pubkey = "MIIBMzCB7AYHKoZIzj0CATCB4AIBATAsBgcqhkjOPQEBAiEA/////v////////////////////8AAAAA//////////8wRAQg/////v////////////////////8AAAAA//////////wEICjp+p6dn140TVqeS89lCafzl4n1FauPkt28vUFNlA6TBEEEMsSuLB8ZgRlfmQRGajnJlI/jC7/yZgvhcVpFiTNMdMe8Nzai9PZ3nFm9zuNraSFT0KmHfMYqR0AC3zLlITnwoAIhAP////7///////////////9yA99rIcYFK1O79Ak51UEjAgEBA0IABPpZ3JJhJqbsNAbllbro4VE0toSVIlko/Z4JanWNL3+zwUo1djeBfQK6bSOh2fy4hODBVEuTvRmrueBy4aksAHM=";
            var privkey = "MIICSwIBADCB7AYHKoZIzj0CATCB4AIBATAsBgcqhkjOPQEBAiEA/////v////////////////////8AAAAA//////////8wRAQg/////v////////////////////8AAAAA//////////wEICjp+p6dn140TVqeS89lCafzl4n1FauPkt28vUFNlA6TBEEEMsSuLB8ZgRlfmQRGajnJlI/jC7/yZgvhcVpFiTNMdMe8Nzai9PZ3nFm9zuNraSFT0KmHfMYqR0AC3zLlITnwoAIhAP////7///////////////9yA99rIcYFK1O79Ak51UEjAgEBBIIBVTCCAVECAQEEIElZYbFSbv17P4+IuORNsDPSlwjRhuVvHF/2KMQzhOvWoIHjMIHgAgEBMCwGByqGSM49AQECIQD////+/////////////////////wAAAAD//////////zBEBCD////+/////////////////////wAAAAD//////////AQgKOn6np2fXjRNWp5Lz2UJp/OXifUVq4+S3by9QU2UDpMEQQQyxK4sHxmBGV+ZBEZqOcmUj+MLv/JmC+FxWkWJM0x0x7w3NqL09necWb3O42tpIVPQqYd8xipHQALfMuUhOfCgAiEA/////v///////////////3ID32shxgUrU7v0CTnVQSMCAQGhRANCAAT6WdySYSam7DQG5ZW66OFRNLaElSJZKP2eCWp1jS9/s8FKNXY3gX0Cum0jodn8uITgwVRLk70Zq7ngcuGpLABz";
            var handle = new Wlniao.Crypto.SM2(pubkey, privkey, Crypto.KeyType.Pkcs8);
            var plainBytes = handle.Decrypt("04c38ac95e4986e2ad2e80354ae375f11015bf2898f1b8dc9a1c8859ed927afafaa237a99f570cafdf61ad9a7dbbc7a9ea60911fdd1c5835344e9ea544b97318c15bc7eff6c50b61a356bf0adbad14b5e3eb1197e93601243b9fba6b8472305967d033550c");

            var plainStr = Encoding.UTF8.GetString(plainBytes);
            // assert
            Assert.IsNotEmpty(plainBytes);
        }

        [Test]
        public void SM2Encrypt()
        {
            var pubkey = "MIIBMzCB7AYHKoZIzj0CATCB4AIBATAsBgcqhkjOPQEBAiEA/////v////////////////////8AAAAA//////////8wRAQg/////v////////////////////8AAAAA//////////wEICjp+p6dn140TVqeS89lCafzl4n1FauPkt28vUFNlA6TBEEEMsSuLB8ZgRlfmQRGajnJlI/jC7/yZgvhcVpFiTNMdMe8Nzai9PZ3nFm9zuNraSFT0KmHfMYqR0AC3zLlITnwoAIhAP////7///////////////9yA99rIcYFK1O79Ak51UEjAgEBA0IABPpZ3JJhJqbsNAbllbro4VE0toSVIlko/Z4JanWNL3+zwUo1djeBfQK6bSOh2fy4hODBVEuTvRmrueBy4aksAHM=";
            var privkey = "MIICSwIBADCB7AYHKoZIzj0CATCB4AIBATAsBgcqhkjOPQEBAiEA/////v////////////////////8AAAAA//////////8wRAQg/////v////////////////////8AAAAA//////////wEICjp+p6dn140TVqeS89lCafzl4n1FauPkt28vUFNlA6TBEEEMsSuLB8ZgRlfmQRGajnJlI/jC7/yZgvhcVpFiTNMdMe8Nzai9PZ3nFm9zuNraSFT0KmHfMYqR0AC3zLlITnwoAIhAP////7///////////////9yA99rIcYFK1O79Ak51UEjAgEBBIIBVTCCAVECAQEEIElZYbFSbv17P4+IuORNsDPSlwjRhuVvHF/2KMQzhOvWoIHjMIHgAgEBMCwGByqGSM49AQECIQD////+/////////////////////wAAAAD//////////zBEBCD////+/////////////////////wAAAAD//////////AQgKOn6np2fXjRNWp5Lz2UJp/OXifUVq4+S3by9QU2UDpMEQQQyxK4sHxmBGV+ZBEZqOcmUj+MLv/JmC+FxWkWJM0x0x7w3NqL09necWb3O42tpIVPQqYd8xipHQALfMuUhOfCgAiEA/////v///////////////3ID32shxgUrU7v0CTnVQSMCAQGhRANCAAT6WdySYSam7DQG5ZW66OFRNLaElSJZKP2eCWp1jS9/s8FKNXY3gX0Cum0jodn8uITgwVRLk70Zq7ngcuGpLABz";
            var handle = new Wlniao.Crypto.SM2(pubkey, privkey, Crypto.KeyType.Pkcs8);
            var plainBytes = handle.Encrypt("345e");

            var plainStr = System.Convert.ToBase64String(plainBytes);
            // assert
            Assert.IsNotEmpty(plainBytes);
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
