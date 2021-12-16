using System.Collections.Generic;
using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace Gandalf.Contracts.Shadowfax
{
    public class ShadowfaxContractInitializationProvider : IContractInitializationProvider
    {
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }

        public Hash SystemSmartContractName { get; } = DAppSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "Gandalf.Contracts.Shadowfax";
    }
}