using System.Numerics;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.IdoContract
{
    public partial class IdoContract
    {
        public override Empty Invest(InvestInput input)
        {
            var offering = State.PublicOfferList.Value.Value[input.PublicId];
            Assert(offering != null, "Activity not exist.");
            Assert(Context.CurrentBlockTime >= offering.StartTime && Context.CurrentBlockTime < offering.EndTime,
                "Not ido time.");
            Assert(offering.OfferingTokenAmount > offering.SubscribedOfferingAmount,
                "Out of stock.");
            var stock = offering.WantTokenAmount.Sub(offering.WantTokenBalance);
            var actualUsed = input.Amount > stock ? stock : input.Amount;
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                Amount = long.Parse(actualUsed.Value),
                From = Context.Sender,
                Symbol = offering.WantTokenSymbol,
                To = Context.Self
            });

            var obtainAmount = offering.OfferingTokenAmount.Mul(actualUsed).Div(offering.WantTokenAmount);

            var userInfo = State.UserInfo[input.PublicId][Context.Sender] ?? new UserInfo
            {
                Claimed = false,
                ObtainAmount = new BigIntValue
                {
                    Value = "0"
                }
            };
            userInfo.ObtainAmount = userInfo.ObtainAmount.Add(obtainAmount);
            State.UserInfo[input.PublicId][Context.Sender] = userInfo;
            offering.WantTokenBalance = offering.WantTokenBalance.Add(actualUsed);
            offering.SubscribedOfferingAmount = offering.SubscribedOfferingAmount.Add(obtainAmount);
            State.PublicOfferList.Value.Value[input.PublicId] = offering;

            Context.Fire(new Invest
            {
                PublicId = input.PublicId,
                Investor = Context.Sender,
                TokenSymbol = offering.WantTokenSymbol,
                Spend = actualUsed,
                Income = obtainAmount
            });
            return new Empty();
        }

        public override Empty Harvest(Int32Value input)
        {
            var offering = State.PublicOfferList.Value.Value[input.Value];
            Assert(offering != null, "Activity not exist.");
            Assert(Context.CurrentBlockTime > offering.EndTime, "The activity is not over.");
            var userInfo = State.UserInfo[input.Value][Context.Sender];
            Assert(userInfo != null, "Not participate in.");
            Assert(!userInfo.Claimed, "Have harvested.");

            userInfo.Claimed = true;
            State.UserInfo[input.Value][Context.Sender] = userInfo;
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Amount = long.Parse(userInfo.ObtainAmount.Value),
                Symbol = offering.OfferingTokenSymbol,
                To = Context.Sender
            });
            
            Context.Fire(new Harvest
            {
                Amount = userInfo.ObtainAmount,
                To = Context.Sender,
                PublicId = input.Value
            });
            return new Empty();
        }
    }
}