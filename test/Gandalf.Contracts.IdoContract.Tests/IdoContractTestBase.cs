using AElf.Boilerplate.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;

namespace Gandalf.Contracts.IdoContract
{
    public class IdoContractTestBase : DAppContractTestBase<IdoContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);

        internal IdoContractContainer.IdoContractStub GetIdoContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<IdoContractContainer.IdoContractStub>(DAppContractAddress, senderKeyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
        }
    }
}