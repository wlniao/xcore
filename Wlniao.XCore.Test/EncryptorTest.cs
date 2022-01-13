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
            var pubkey = "04DFA630B7BAB34FA0EB244DD2A76CC8A19D8EEF7B31C2BEB66E1F6A2977E7048DCF14D2DC3DA74C45DA65F0D487A126436FF20FF4B35C3B00B6325038C4A88543";
            var privkey= "4C2C166B263D9110BC23641721903339517D7279F57DC074773DE0141AFF68A6";
            var handle = new Wlniao.Crypto.SM2(pubkey, privkey, Crypto.KeyType.Generate);
            var plainBytes = Encoding.UTF8.GetBytes("abcdefg");
            var signBytes = handle.Sign(plainBytes);
            var signStr = cvt.BytesToHexString(signBytes);
            signStr = "304402205b84e2244742157527a30303006a759d36e6930777a8b46cafc99c47b878c562022070923441cb95380dcbd8af7753cfd0a2f06b9612bc718760ccd4a86bdc425ef4";
            var verifySingResult = handle.VerifySign(plainBytes, cvt.HexStringToBytes(signStr));

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
