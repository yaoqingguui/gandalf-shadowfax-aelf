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

namespace Gandalf.Contracts.Shadowfax
{
    public class ShadowfaxContractTests : ShadowfaxContractTestBase
    {
        private Address _owner;
        private ECKeyPair _ownerKeyPair;
        private Address _kitty;
        private ECKeyPair _kittyKeyPair;
        private Address _tom;
        private ECKeyPair _tomKeyPair;

        private const string TokenOfferSymbol = "PANDA";
        private const string TokenWantSymbol = "CAKE";

        [Fact]
        public async Task Init_Success_Test()
        {
            var stub = await InitializeGame();
            var addr = await stub.Owner.SendAsync(new Empty());
            addr.Output.ShouldBe(_kitty);
        }

        [Fact]
        public async Task init_Fail_Test()
        {
            _ownerKeyPair = SampleAccount.Accounts.First().KeyPair;
            var stub = GetShadowfaxContractStub(_ownerKeyPair);
            await stub.Initialize.SendAsync(new InitializeInput());
            var owenr = await stub.Owner.CallAsync(new Empty());
            owenr.ShouldBe(Address.FromPublicKey(_ownerKeyPair.PublicKey));

            // await stub.Initialize.SendAsync(SampleAccount.Accounts[1].Address);
            (await Assert.ThrowsAsync<Exception>(() =>
                    stub.Initialize.SendAsync(new InitializeInput
                    {
                        Owner = SampleAccount.Accounts[1].Address
                    }))).Message
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

            var tomStub = GetShadowfaxContractStub(_tomKeyPair);
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(1000).ToTimestamp();
            await AddPublicOffering(startTime, endTime);

            var offering = await tomStub.PublicOfferings.CallAsync(new Int64Value
            {
                Value = 0
            });

            offering.OfferingTokenSymbol.ShouldBe(TokenOfferSymbol);
            offering.OfferingTokenAmount.ShouldBe(100000);
            offering.Claimed.ShouldBe(false);
            offering.Publisher.ShouldBe(_tom);
            offering.StartTime.ShouldBe(startTime);
            offering.EndTime.ShouldBe(endTime);
            offering.WantTokenSymbol.ShouldBe(TokenWantSymbol);
            offering.WantTokenAmount.ShouldBe(20000);
            offering.SubscribedOfferingAmount.ShouldBe(0);
            offering.WantTokenBalance.ShouldBe(0);
            offering.PublicId.ShouldBe(0);

            var int32Value = await tomStub.GetPublicOfferingLength.CallAsync(new Empty());
            int32Value.Value.ShouldBe(1);
        }

        [Fact]
        public async Task Invest_Should_Fail_Withdrawout_Add_Offering_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var kittyStub = GetShadowfaxContractStub(_kittyKeyPair);
            var tokenKitty = GetTokenContractStub(_kittyKeyPair);
            await tokenKitty.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContractAddress,
                Amount = 200000,
                Symbol = TokenWantSymbol
            });

            (await Assert.ThrowsAsync<Exception>(() => kittyStub.Invest.SendAsync(new InvestInput
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
            var tomStub = GetShadowfaxContractStub(_tomKeyPair);
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            await AddPublicOffering(startTime, endTime);
            var offering = await tomStub.PublicOfferings.CallAsync(new Int64Value
            {
                Value = 0
            });
            offering.PublicId.ShouldBe(0);

            var kittyStub = GetShadowfaxContractStub(_kittyKeyPair);
            var tokenKitty = GetTokenContractStub(_kittyKeyPair);
            var wantTokenBalanceOfKitty = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _kitty,
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
                Owner = _kitty,
                Symbol = TokenWantSymbol
            });
            wantTokenBalanceOfKitty.Balance.ShouldBe(1000000 - 2000);
            offering = await tomStub.PublicOfferings.CallAsync(new Int64Value
            {
                Value = 0
            });
            offering.WantTokenBalance.ShouldBe(2000);
            var obtainAmount = offering.OfferingTokenAmount.Mul(2000).Div(offering.WantTokenAmount);

            offering.SubscribedOfferingAmount.ShouldBe(obtainAmount);
            var userInfo = await kittyStub.UserInfo.CallAsync(new UserInfoInput
            {
                User = _kitty,
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
                Owner = _kitty,
                Symbol = TokenWantSymbol
            });

            wantTokenBalanceOfKitty.Balance.ShouldBe(1000000 - 2000 - 18000);
            offering = await tomStub.PublicOfferings.CallAsync(new Int64Value
            {
                Value = 0
            });
            offering.WantTokenBalance.ShouldBe(20000);

            userInfo = await kittyStub.UserInfo.CallAsync(new UserInfoInput
            {
                User = _kitty,
                PublicId = 0
            });
            userInfo.Claimed.ShouldBe(false);
            userInfo.ObtainAmount.ShouldBe(100000);

            await Task.Delay(60 * 1000);

            var offerTokenbalance = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _kitty,
                Symbol = TokenOfferSymbol
            });
            offerTokenbalance.Balance.ShouldBe(0);

            await kittyStub.Harvest.SendAsync(new Int64Value
            {
                Value = 0
            });

            userInfo = await kittyStub.UserInfo.CallAsync(new UserInfoInput
            {
                User = _kitty,
                PublicId = 0
            });

            userInfo.Claimed.ShouldBe(true);
            offerTokenbalance = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _kitty,
                Symbol = TokenOfferSymbol
            });

            offerTokenbalance.Balance.ShouldBe(100000);
        }

        [Fact]
        public async Task Pulisher_Raise_Full_And_Withdraw_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            await AddPublicOffering(startTime, endTime);
            var tokenKitty = GetTokenContractStub(_kittyKeyPair);
            var kittyStub = GetShadowfaxContractStub(_kittyKeyPair);
            await tokenKitty.Approve.SendAsync(new ApproveInput
            {
                Amount = 20000,
                Spender = DAppContractAddress,
                Symbol = TokenWantSymbol,
            });
            await Task.Delay(2000);
            await kittyStub.Invest.SendAsync(new InvestInput
            {
                Amount = 20000,
                PublicId = 0
            });

            var tomStub = GetShadowfaxContractStub(_tomKeyPair);

            (await Assert.ThrowsAsync<Exception>(() => tomStub.Withdraw.SendAsync(new Int64Value
            {
                Value = 0
            }))).Message.ShouldContain("Game not over.");

            await Task.Delay(60 * 1000);
            await tomStub.Withdraw.SendAsync(new Int64Value
            {
                Value = 0
            });

            var balanceCallAsync = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _tom,
                Symbol = TokenWantSymbol
            });
            balanceCallAsync.Balance.ShouldBe(20000);
        }

        [Fact]
        public async Task Publisher_Should_Withdraw_Success_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            await AddPublicOffering(startTime, endTime);

            var tokenKitty = GetTokenContractStub(_kittyKeyPair);
            var kittyStub = GetShadowfaxContractStub(_kittyKeyPair);

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

            var offering = await kittyStub.PublicOfferings.CallAsync(new Int64Value
            {
                Value = 0
            });
            offering.Claimed.ShouldBe(false);
            offering.Publisher.ShouldBe(_tom);
            offering.WantTokenBalance.ShouldBe(2000);

            (await Assert.ThrowsAsync<Exception>(() => kittyStub.Withdraw.SendAsync(new Int64Value
            {
                Value = 0
            }))).Message.ShouldContain("No rights.");
            await Task.Delay(60 * 1000);

            var tomStub = GetShadowfaxContractStub(_tomKeyPair);
            await tomStub.Withdraw.SendAsync(new Int64Value
            {
                Value = 0
            });
            var tomWantTokenBalance = await tokenKitty.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _tom,
                Symbol = TokenWantSymbol
            });
            tomWantTokenBalance.Balance.ShouldBe(2000);
        }

        [Fact]
        public async Task Publisher_Should_Change_Ascription_Test()
        {
            await InitializeGame();
            await ResetTimeSpan();
            var tokenTomStub = GetTokenContractStub(_tomKeyPair);
            await tokenTomStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 20000,
                Symbol = TokenOfferSymbol,
                To = _kitty
            });

            var kittyOfferingTokenBalacne = await tokenTomStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _kitty,
                Symbol = TokenOfferSymbol
            });
            kittyOfferingTokenBalacne.Balance.ShouldBe(20000);

            var startTime = DateTime.UtcNow.AddSeconds(1).ToTimestamp();
            var endTime = DateTime.UtcNow.AddSeconds(60).ToTimestamp();
            await AddPublicOffering(startTime, endTime);
            var kittyStub = GetKittyStub();
            var tokenOwner = await kittyStub.Result.Ascription.CallAsync(new StringValue
            {
                Value = TokenOfferSymbol
            });
            tokenOwner.Value.ShouldBe(_tom.Value);
            var tokenKittyStub = GetTokenContractStub(_kittyKeyPair);
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

            (await Assert.ThrowsAsync<Exception>(() => kittyStub.Result.ChangeAscription.SendAsync(
                new ChangeAscriptionInput
                {
                    Receiver = _tom,
                    TokenSymbol = TokenOfferSymbol
                }))).Message.ShouldContain("No right to assign.");

            var tomStub = GetShadowfaxContractStub(_tomKeyPair);
            await tomStub.ChangeAscription.SendAsync(new ChangeAscriptionInput
            {
                Receiver = _kitty,
                TokenSymbol = TokenOfferSymbol
            });

            tokenOwner = await kittyStub.Result.Ascription.CallAsync(new StringValue
            {
                Value = TokenOfferSymbol
            });

            tokenOwner.Value.ShouldBe(_kitty.Value);
            await kittyStub.Result.AddPublicOffering.SendAsync(new AddPublicOfferingInput
            {
                StartTime = startTime,
                EndTime = endTime,
                OfferingTokenAmount = 20000,
                OfferingTokenSymbol = TokenOfferSymbol,
                WantTokenAmount = 2000,
                WantTokenSymbol = TokenWantSymbol
            });

            var kittyOffering = await kittyStub.Result.PublicOfferings.CallAsync(new Int64Value
            {
                Value = 1
            });

            kittyOffering.Publisher.ShouldBe(_kitty);
        }

        private async Task AddPublicOffering(Timestamp startTime, Timestamp endTime)
        {
            var tomStub = GetShadowfaxContractStub(_tomKeyPair);
            var offeringStub = GetTokenContractStub(_tomKeyPair);
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

            var minimalTimespan = await stub.MinimalTimespan.CallAsync(new Empty());
            var maximalTimeSpan = await stub.MaximalTimeSpan.CallAsync(new Empty());

            maximalTimeSpan.Value.ShouldBe(1000);
            minimalTimespan.Value.ShouldBe(50);
        }

        private async Task CreateToken()
        {
            var wantToken = GetTokenContractStub(_kittyKeyPair);
            await wantToken.Create.SendAsync(new CreateInput
            {
                Decimals = 1,
                Symbol = TokenWantSymbol,
                Issuer = _kitty,
                IsBurnable = false,
                TokenName = TokenWantSymbol,
                TotalSupply = 1000000,
            });
            await wantToken.Issue.SendAsync(new IssueInput
            {
                Amount = 1000000,
                Symbol = TokenWantSymbol,
                To = _kitty
            });

            var cakeBalance = await wantToken.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _kitty,
                Symbol = TokenWantSymbol
            });
            cakeBalance.Balance.ShouldBe(1000000);
            var offeringToken = GetTokenContractStub(_tomKeyPair);
            await offeringToken.Create.SendAsync(new CreateInput
            {
                Decimals = 1,
                Issuer = _tom,
                Symbol = TokenOfferSymbol,
                TokenName = TokenOfferSymbol,
                TotalSupply = 500000
            });
            await offeringToken.Issue.SendAsync(new IssueInput
            {
                Amount = 500000,
                Symbol = TokenOfferSymbol,
                To = _tom
            });
            var pandaBalance = await offeringToken.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = _tom,
                Symbol = TokenOfferSymbol
            });
            pandaBalance.Balance.ShouldBe(500000);
        }


        private async Task<ShadowfaxContractContainer.ShadowfaxContractStub> GetKittyStub()
        {
            return GetShadowfaxContractStub(_kittyKeyPair);
        }

        private async Task<ShadowfaxContractContainer.ShadowfaxContractStub> InitializeGame()
        {
            _ownerKeyPair = SampleAccount.Accounts.First().KeyPair;
            _owner = Address.FromPublicKey(_ownerKeyPair.PublicKey);
            _kittyKeyPair = SampleAccount.Accounts[1].KeyPair;
            _kitty = Address.FromPublicKey(_kittyKeyPair.PublicKey);
            _tomKeyPair = AElf.ContractTestKit.SampleAccount.Accounts[2].KeyPair;
            _tom = Address.FromPublicKey(_tomKeyPair.PublicKey);
            var stub = GetShadowfaxContractStub(_ownerKeyPair);
            await stub.Initialize.SendAsync(new InitializeInput
            {
                Owner = _kitty
            });
            await CreateToken();
            return stub;
        }
    }
}