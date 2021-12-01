using AElf.Boilerplate.TestBase;
using AElf.Cryptography.ECDSA;

namespace IdoContract
{
    public class IdoContractTestBase : DAppContractTestBase<IdoContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);

        internal IdoContractContainer.IdoContractStub GetIdoContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<IdoContractContainer.IdoContractStub>(DAppContractAddress, senderKeyPair);
        }
    }
}