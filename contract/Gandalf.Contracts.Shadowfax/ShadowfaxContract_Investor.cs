using System.Numerics;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.Shadowfax
{
    public partial class ShadowfaxContract
    {
        public override Empty Invest(InvestInput input)
        {
            Assert(input.Channel != null, "Invalid channel.");
            var offering = GetOffering(input.PublicId);
            Assert(Context.CurrentBlockTime >= offering.StartTime && Context.CurrentBlockTime < offering.EndTime,
                "Not ido time.");
            Assert(offering.OfferingTokenAmount > offering.SubscribedOfferingAmount,
                "Out of stock.");
            var stock = offering.WantTokenAmount.Sub(offering.WantTokenBalance);
            var actualUsed = input.Amount > stock ? stock : input.Amount;
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                Amount = actualUsed,
                From = Context.Sender,
                Symbol = offering.WantTokenSymbol,
                To = Context.Self
            });

            var obtainAmount = offering.OfferingTokenAmount.Mul(actualUsed).Div(offering.WantTokenAmount);

            var userInfo = State.UserInfo[input.PublicId][Context.Sender] ?? new UserInfo
            {
                Claimed = false
            };
            userInfo.ObtainAmount = userInfo.ObtainAmount.Add(obtainAmount);
            State.UserInfo[input.PublicId][Context.Sender] = userInfo;
            offering.WantTokenBalance = offering.WantTokenBalance.Add(actualUsed);
            offering.SubscribedOfferingAmount = offering.SubscribedOfferingAmount.Add(obtainAmount);
            State.PublicOfferingMap[input.PublicId] = offering;

            Context.Fire(new Invest
            {
                PublicId = input.PublicId,
                Investor = Context.Sender,
                TokenSymbol = offering.WantTokenSymbol,
                Spend = actualUsed,
                Income = obtainAmount,
                Channel = input.Channel
            });
            return new Empty();
        }

        public override Empty Harvest(Int64Value input)
        {
            var offering = GetOffering(input.Value);
            Assert(Context.CurrentBlockTime > offering.EndTime, "The activity is not over.");
            var userInfo = State.UserInfo[input.Value][Context.Sender];
            Assert(userInfo != null, "Not participate in.");
            Assert(!userInfo.Claimed, "Have harvested.");

            userInfo.Claimed = true;
            State.UserInfo[input.Value][Context.Sender] = userInfo;
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Amount = userInfo.ObtainAmount,
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

        private PublicOffering GetOffering(long index)
        {
            Assert(State.CurrentPublicOfferingId.Value > index, "Activity id not exist.");
            var offering = State.PublicOfferingMap[index];
            Assert(offering != null, "Activity not exist.");
            return offering;
        }
    }
}