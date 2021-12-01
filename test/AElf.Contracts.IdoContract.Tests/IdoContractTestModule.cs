using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.IdoContract;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Gandalf.Contracts.IdoContract
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class IdoContractTestModule : MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IContractInitializationProvider, IdoContractInitializationProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            var contractDllLocation = typeof(IdoContract).Assembly.Location;
            var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
            {
                {
                    new IdoContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(contractDllLocation)
                }
            };
            contractCodeProvider.Codes = contractCodes;
        }
    }
}