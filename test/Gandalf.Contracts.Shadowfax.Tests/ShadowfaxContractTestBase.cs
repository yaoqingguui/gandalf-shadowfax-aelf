using AElf.Boilerplate.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;

namespace Awaken.Contracts.Shadowfax
{
    public class ShadowfaxContractTestBase : DAppContractTestBase<ShadowfaxContractTestModule>
    {
        internal ShadowfaxContractContainer.ShadowfaxContractStub GetShadowfaxContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<ShadowfaxContractContainer.ShadowfaxContractStub>(DAppContractAddress, senderKeyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
        }
    }
}