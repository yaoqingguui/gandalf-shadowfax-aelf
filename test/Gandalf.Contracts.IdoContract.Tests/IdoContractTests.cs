using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Math;
using Shouldly;
using Virgil.Crypto;
using Xunit;

namespace Gandalf.Contracts.IdoContract
{
    public class IdoContractTests : IdoContractTestBase
    {
        public Address Owner;
        public ECKeyPair OwnerKeyPair;
        public Address Kitty;
        public ECKeyPair KittyKeyPair;
        public Address Tom;
        public ECKeyPair TomKeyPair;


        [Fact]
        public async Task Test()
        {
            // Get a stub for testing.
            var keyPair = SampleAccount.Accounts.First().KeyPair;
            var stub = GetIdoContractStub(keyPair);

            // Use CallAsync or SendAsync method of this stub to test.
            // await stub.Hello.SendAsync(new Empty())

            // Or maybe you want to get its return value.
            // var output = (await stub.Hello.SendAsync(new Empty())).Output;

            // Or transaction result.
            // var transactionResult = (await stub.Hello.SendAsync(new Empty())).TransactionResult;
        }

        [Fact]
        public async Task init_Success_Test()
        {
            var stub = await initializeGame();
            var addr = await stub.GetOwner.SendAsync(new Empty());
            addr.Output.ShouldBe(Kitty);
        }

        [Fact]
        public async Task init_Fail_Test()
        {
            var stub = GetIdoContractStub(OwnerKeyPair);
            await stub.Initialize.SendAsync(new Address());
            var owenr = await stub.GetOwner.CallAsync(new Empty());
            owenr.ShouldBe(Address.FromPublicKey(OwnerKeyPair.PublicKey));

            // await stub.Initialize.SendAsync(SampleAccount.Accounts[1].Address);
            (await Assert.ThrowsAsync<Exception>(() =>
                    stub.Initialize.SendAsync(SampleAccount.Accounts[1].Address))).Message
                .ShouldContain("Already initialized.");
        }


        [Fact]
        public async Task ResetTimeSpan_Test()
        {
            await initializeGame();
            await ResetTimeSpan();
        }


        [Fact]
        public async Task Add_Public_Offering_Test()
        {
            await initializeGame();
            await ResetTimeSpan();
            var stub = await getKittyStub();
            var offeringStub = GetTokenContractStub(KittyKeyPair);
            await offeringStub.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContractAddress,
                Symbol = "CAKE",
                Amount = long.MaxValue,
            });

            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(1000).ToTimestamp();
            await stub.AddPublicOffering.SendAsync(new AddPublicOfferingInput
            {
                StartTime = startTime,
                EndTime = endTime,
                OfferingTokenSymbol = "CAKE",
                OfferingTokenAmount = 100000,
                WantTokenAmount = 20000,
                WantTokenSymbol = "PANDA",
            });

            var offering = await stub.GetPublicOffering.CallAsync(new Int32Value
            {
                Value = 0
            });

            offering.OfferingTokenSymbol.ShouldBe("CAKE");
        }


        private async Task ResetTimeSpan()
        {
            var stub = await getKittyStub();
            await stub.ResetTimeSpan.SendAsync(new ResetTimeSpanInput
            {
                MaxTimespan = 1000,
                MinTimespan = 50
            });

            var output = await stub.GetTimespan.CallAsync(new Empty());
            output.MaxTimespan.ShouldBe(1000);
            output.MinTimespan.ShouldBe(50);
        }


        private async Task createToken()
        {
            var wantToken = GetTokenContractStub(KittyKeyPair);
            await wantToken.Create.SendAsync(new CreateInput
            {
                Decimals = 1,
                Symbol = "CAKE",
                Issuer = Kitty,
                IsBurnable = false,
                TokenName = "CAKE",
                TotalSupply = 1000000,
            });

            var offeringToken = GetTokenContractStub(TomKeyPair);
            await offeringToken.Create.SendAsync(new CreateInput
            {
                Decimals = 1,
                Issuer = Tom,
                Symbol = "PANDA",
                TokenName = "PANDA",
                TotalSupply = 500000
            });
        }

        private async Task<IdoContractContainer.IdoContractStub> getKittyStub()
        {
            return GetIdoContractStub(KittyKeyPair);
        }

        private async Task<IdoContractContainer.IdoContractStub> initializeGame()
        {
            OwnerKeyPair = SampleAccount.Accounts.First().KeyPair;
            Owner = Address.FromPublicKey(OwnerKeyPair.PublicKey);
            KittyKeyPair = SampleAccount.Accounts[1].KeyPair;
            Kitty = Address.FromPublicKey(KittyKeyPair.PublicKey);
            TomKeyPair = AElf.ContractTestKit.SampleAccount.Accounts[2].KeyPair;
            Tom = Address.FromPublicKey(TomKeyPair.PublicKey);
            var stub = GetIdoContractStub(OwnerKeyPair);
            await stub.Initialize.SendAsync(Kitty);
            await createToken();
            return stub;
        }
    }
}