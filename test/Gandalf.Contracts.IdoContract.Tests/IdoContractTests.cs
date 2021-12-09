using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
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

        public const string TokenOfferSymbol = "PANDA";
        public const string TokenWantSymbol = "CAKE";

        [Fact]
        public async Task init_Success_Test()
        {
            var stub = await InitializeGame();
            var addr = await stub.GetOwner.SendAsync(new Empty());
            addr.Output.ShouldBe(Kitty);
        }

        [Fact]
        public async Task init_Fail_Test()
        {   
            OwnerKeyPair = SampleAccount.Accounts.First().KeyPair;
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
            await InitializeGame();
            await ResetTimeSpan();
        }


        [Fact]
        public async Task Add_Public_Offering_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();

            var tomStub = GetIdoContractStub(TomKeyPair);
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(1000).ToTimestamp();
            await AddPublicOffering(startTime, endTime);

            var offering = await tomStub.GetPublicOffering.CallAsync(new Int32Value
            {
                Value = 0
            });

            offering.OfferingTokenSymbol.ShouldBe(TokenOfferSymbol);
            offering.OfferingTokenAmount.ShouldBe(100000);
            offering.Claimed.ShouldBe(false);
            offering.Publisher.ShouldBe(Tom);
            offering.StartTime.ShouldBe(startTime);
            offering.EndTime.ShouldBe(endTime);
            offering.WantTokenSymbol.ShouldBe(TokenWantSymbol);
            offering.WantTokenAmount.ShouldBe(20000);
            offering.SubscribedOfferingAmount.ShouldBe(0);
            offering.WantTokenBalance.ShouldBe(0);
            offering.PublicId.ShouldBe(0);
        }
        
        [Fact]
        public async  Task Invest_Should_Fail_Withdrawout_Add_Offering_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var kittyStub = GetIdoContractStub(KittyKeyPair);
            var tokenKitty = GetTokenContractStub(KittyKeyPair);
            await tokenKitty.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContractAddress,
                Amount = 200000,
                Symbol = TokenWantSymbol
            });

            (await Assert.ThrowsAsync<Exception>(() =>  kittyStub.Invest.SendAsync(new InvestInput
            {
                PublicId = 0,
                Amount = 2000
            }))).Message.ShouldContain("Activity id not exist.");
        }

        [Fact]
        public async Task Invest_And_Harvest_Should_Success_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var tomStub = GetIdoContractStub(TomKeyPair);
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            await AddPublicOffering(startTime, endTime);
            var offering = await tomStub.GetPublicOffering.CallAsync(new Int32Value
            {
                Value = 0
            });
            offering.PublicId.ShouldBe(0);

            var kittyStub = GetIdoContractStub(KittyKeyPair);
            var tokenKitty = GetTokenContractStub(KittyKeyPair);
            var wantTokenBalanceOfKitty = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Kitty,
                Symbol = TokenWantSymbol
            });
            wantTokenBalanceOfKitty.Balance.ShouldBe(1000000);

            await Task.Delay(2000);
            await tokenKitty.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContractAddress,
                Amount = 200000,
                Symbol = TokenWantSymbol
            });
            await kittyStub.Invest.SendAsync(new InvestInput
            {
                PublicId = 0,
                Amount = 2000
            });


            wantTokenBalanceOfKitty = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Kitty,
                Symbol = TokenWantSymbol
            });
            wantTokenBalanceOfKitty.Balance.ShouldBe(1000000 - 2000);
            offering = await tomStub.GetPublicOffering.CallAsync(new Int32Value
            {
                Value = 0
            });
            offering.WantTokenBalance.ShouldBe(2000);
            var obtainAmount = offering.OfferingTokenAmount.Mul(2000).Div(offering.WantTokenAmount);

            offering.SubscribedOfferingAmount.ShouldBe(obtainAmount);
            var userInfo = await kittyStub.GetUserInfo.CallAsync(new UserInfoInput
            {
                User = Kitty,
                PublicId = 0
            });
            userInfo.Claimed.ShouldBe(false);
            userInfo.ObtainAmount.ShouldBe(obtainAmount);

            // Invest again (invest 19000,but stock only surplus 18000 )
            await kittyStub.Invest.SendAsync(new InvestInput
            {
                Amount = 19000,
                PublicId = 0
            });
            wantTokenBalanceOfKitty = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Kitty,
                Symbol = TokenWantSymbol
            });

            wantTokenBalanceOfKitty.Balance.ShouldBe(1000000 - 2000 - 18000);
            offering = await tomStub.GetPublicOffering.CallAsync(new Int32Value
            {
                Value = 0
            });
            offering.WantTokenBalance.ShouldBe(20000);

            userInfo = await kittyStub.GetUserInfo.CallAsync(new UserInfoInput
            {
                User = Kitty,
                PublicId = 0
            });
            userInfo.Claimed.ShouldBe(false);
            userInfo.ObtainAmount.ShouldBe(100000);

            await Task.Delay(60 * 1000);
            
            var offerTokenbalance = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Kitty,
                Symbol = TokenOfferSymbol
            });
            offerTokenbalance.Balance.ShouldBe(0); 
            
            await kittyStub.Harvest.SendAsync(new Int32Value
            {
                Value = 0
            });

            userInfo = await kittyStub.GetUserInfo.CallAsync(new UserInfoInput
            {
                User = Kitty,
                PublicId = 0
            });

            userInfo.Claimed.ShouldBe(true);
            offerTokenbalance = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Kitty,
                Symbol = TokenOfferSymbol
            });

            offerTokenbalance.Balance.ShouldBe(100000);
        }

        [Fact]
        public async Task Publisher_Should_Withdraw_Success_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            await AddPublicOffering(startTime, endTime);

            var tokenKitty = GetTokenContractStub(KittyKeyPair);
            var kittyStub = GetIdoContractStub(KittyKeyPair);

            await tokenKitty.Approve.SendAsync(new ApproveInput
            {
                Amount = 20000,
                Spender = DAppContractAddress,
                Symbol = TokenWantSymbol,
            });

            await Task.Delay(2000);
            await kittyStub.Invest.SendAsync(new InvestInput
            {
                Amount = 2000,
                PublicId = 0
            });
            
            var offering = await kittyStub.GetPublicOffering.CallAsync(new Int32Value
            {
                Value = 0
            });
            offering.Claimed.ShouldBe(false);
            offering.Publisher.ShouldBe(Tom);
            offering.WantTokenBalance.ShouldBe(2000);

            (await Assert.ThrowsAsync<Exception>(() => kittyStub.Withdraw.SendAsync(new Int32Value
            {
                Value = 0
            }))).Message.ShouldContain( "No rights.");
        }
        
        [Fact]
        public async Task Publisher_Should_Change_Ascription_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var tokenTomStub = GetTokenContractStub(TomKeyPair);
            await tokenTomStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 20000,
                Symbol = TokenOfferSymbol,
                To = Kitty
            });

            var kittyOfferingTokenBalacne = await tokenTomStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Kitty,
                Symbol = TokenOfferSymbol
            });
            kittyOfferingTokenBalacne.Balance.ShouldBe(20000);
            
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            await AddPublicOffering(startTime, endTime);
            var kittyStub = GetKittyStub();
            var tokenKittyStub = GetTokenContractStub(KittyKeyPair);
            await tokenKittyStub.Approve.SendAsync(new ApproveInput
            {
                Amount = 20000,
                Spender = DAppContractAddress,
                Symbol = TokenOfferSymbol
            });

            (await Assert.ThrowsAsync<Exception>(() => kittyStub.Result.AddPublicOffering.SendAsync(
                new AddPublicOfferingInput
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    OfferingTokenAmount = 20000,
                    OfferingTokenSymbol = TokenOfferSymbol,
                    WantTokenAmount = 2000,
                    WantTokenSymbol = TokenWantSymbol
                }))).Message.ShouldContain("Another has published the token before.");

            var tomStub = GetIdoContractStub(TomKeyPair);
            await tomStub.ChangeAscription.SendAsync(new ChangeAscriptionInput
            {
                Receiver = Kitty,
                TokenSymbol = TokenOfferSymbol
            });

            await kittyStub.Result.AddPublicOffering.SendAsync(new AddPublicOfferingInput
            {
                StartTime = startTime,
                EndTime = endTime,
                OfferingTokenAmount = 20000,
                OfferingTokenSymbol = TokenOfferSymbol,
                WantTokenAmount = 2000,
                WantTokenSymbol = TokenWantSymbol
            });

            var kittyOffering = await kittyStub.Result.GetPublicOffering.CallAsync(new Int32Value
            {
                Value = 1
            });
            
            kittyOffering.Publisher.ShouldBe(Kitty);
        }
        
        
        
        
        private async Task AddPublicOffering(Timestamp startTime, Timestamp endTime)
        {
            var tomStub = GetIdoContractStub(TomKeyPair);
            var offeringStub = GetTokenContractStub(TomKeyPair);
            await offeringStub.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContractAddress,
                Symbol = TokenOfferSymbol,
                Amount = long.MaxValue,
            });


            await tomStub.AddPublicOffering.SendAsync(new AddPublicOfferingInput
            {
                StartTime = startTime,
                EndTime = endTime,
                OfferingTokenSymbol = TokenOfferSymbol,
                OfferingTokenAmount = 100000,
                WantTokenAmount = 20000,
                WantTokenSymbol = TokenWantSymbol,
            });
        }

        private async Task ResetTimeSpan()
        {
            var stub = await GetKittyStub();
            await stub.ResetTimeSpan.SendAsync(new ResetTimeSpanInput
            {
                MaxTimespan = 1000,
                MinTimespan = 50
            });

            var output = await stub.GetTimespan.CallAsync(new Empty());
            output.MaxTimespan.ShouldBe(1000);
            output.MinTimespan.ShouldBe(50);
        }


        private async Task CreateToken()
        {
            var wantToken = GetTokenContractStub(KittyKeyPair);
            await wantToken.Create.SendAsync(new CreateInput
            {
                Decimals = 1,
                Symbol = TokenWantSymbol,
                Issuer = Kitty,
                IsBurnable = false,
                TokenName = TokenWantSymbol,
                TotalSupply = 1000000,
            });
            await wantToken.Issue.SendAsync(new IssueInput
            {
                Amount = 1000000,
                Symbol = TokenWantSymbol,
                To = Kitty
            });

            var cakeBalance = await wantToken.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Kitty,
                Symbol = TokenWantSymbol
            });
            cakeBalance.Balance.ShouldBe(1000000);
            var offeringToken = GetTokenContractStub(TomKeyPair);
            await offeringToken.Create.SendAsync(new CreateInput
            {
                Decimals = 1,
                Issuer = Tom,
                Symbol = TokenOfferSymbol,
                TokenName = TokenOfferSymbol,
                TotalSupply = 500000
            });
            await offeringToken.Issue.SendAsync(new IssueInput
            {
                Amount = 500000,
                Symbol = TokenOfferSymbol,
                To = Tom
            });
            var pandaBalance = await offeringToken.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Tom,
                Symbol = TokenOfferSymbol
            });
            pandaBalance.Balance.ShouldBe(500000);
        }
        
        
        private async Task<IdoContractContainer.IdoContractStub> GetKittyStub()
        {
            return GetIdoContractStub(KittyKeyPair);
        }

        private async Task<IdoContractContainer.IdoContractStub> InitializeGame()
        {
            OwnerKeyPair = SampleAccount.Accounts.First().KeyPair;
            Owner = Address.FromPublicKey(OwnerKeyPair.PublicKey);
            KittyKeyPair = SampleAccount.Accounts[1].KeyPair;
            Kitty = Address.FromPublicKey(KittyKeyPair.PublicKey);
            TomKeyPair = AElf.ContractTestKit.SampleAccount.Accounts[2].KeyPair;
            Tom = Address.FromPublicKey(TomKeyPair.PublicKey);
            var stub = GetIdoContractStub(OwnerKeyPair);
            await stub.Initialize.SendAsync(Kitty);
            await CreateToken();
            return stub;
        }
    }
}