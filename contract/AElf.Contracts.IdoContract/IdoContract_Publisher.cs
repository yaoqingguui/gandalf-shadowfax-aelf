using System.Numerics;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.IdoContract
{
    public partial class IdoContract
    {
        public override Int64Value AddPublicOffering(AddPublicOfferingInput input)
        {   
            Assert((BigInteger) input.OfferingTokenAmount > 0, "Need deposit some offering token.");
            Assert(input.StartTime >= Context.CurrentBlockTime, "Invaild start time.");
            Assert(input.EndTime.Seconds <= input.StartTime.Seconds + State.MaximalTimeSpan.Value ||
                   input.EndTime.Seconds >= input.StartTime.Seconds + State.MinimalTimespan.Value, "Invaild end time.");
            var owner = State.Ascription[input.OfferingTokenSymbol];
            if (owner != null)
            {
                Assert(owner == Context.Sender, "Another has published the token before.");
            }
            else
            {
                State.Ascription[input.OfferingTokenSymbol] = Context.Sender;
            }

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = long.Parse(input.OfferingTokenAmount.Value),
                Symbol = input.OfferingTokenSymbol
            });

            var publicOfferList = State.PublicOfferList.Value ?? new PublicOfferList();
            publicOfferList.Value.Add(new PublicOffering
            {
                OfferingTokenSymbol = input.OfferingTokenSymbol,
                OfferingTokenAmount = input.OfferingTokenAmount,
                WantTokenSymbol = input.WantTokenSymbol,
                WantTokenAmount = input.WantTokenAmount,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                Publisher = Context.Sender,
                Claimed = false,
                WantTokenBalance = 0,
                SubscribedOfferingAmount = 0
            });
            
            State.PublicOfferList.Value = publicOfferList;
            var publicId = publicOfferList.Value.Count - 1;
            Context.Fire(new AddPublicOffering
            {
                OfferingTokenSymbol = input.OfferingTokenSymbol,
                OfferingTokenAmount = input.OfferingTokenAmount,
                WantTokenSymbol = input.WantTokenSymbol,
                WantTokenAmount = input.WantTokenAmount,
                Publisher = Context.Sender,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                PublicId = publicId
            });
            return new Int64Value
            {
                Value = publicId
            };
        }


        public override Empty ChangeAscription(ChangeAscriptionInput input)
        {   
            Assert(State.Ascription[input.TokenSymbol] == Context.Sender, "No right to assign.");
            State.Ascription[input.TokenSymbol] = input.Receiver;
            Context.Fire(new ChangeAscription
            {
                TokenSymbol = input.TokenSymbol,
                OldPublisher = Context.Sender,
                NewPublisher = input.Receiver
            });
            return new Empty();
        }


        public override Empty Withdraw(Int32Value input)
        {
            Assert(input.Value >= 0, "Invalid number.");
            var offering = State.PublicOfferList.Value.Value[input.Value];
            Assert(!offering.Claimed,"Have withdrawn.");
            Assert(offering.Publisher==Context.Sender,"No rights.");
            Assert(Context.CurrentBlockTime>offering.EndTime,"Game not over.");
            offering.Claimed = true;
            BigInteger wantTokenBalance = offering.WantTokenBalance;
            BigInteger surplus = offering.OfferingTokenAmount.Sub(offering.SubscribedOfferingAmount);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Amount = (long) wantTokenBalance,
                Symbol = offering.WantTokenSymbol
            });
           
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Amount = (long)surplus,
                Symbol = offering.OfferingTokenSymbol,
                To = Context.Sender,
            });
            
            Context.Fire( new Withdraw
            {
                PubilicId = input.Value,
                To = Context.Sender,
                OfferingToken = (long) surplus,
                WantToken = (long) wantTokenBalance
            });
            return new Empty();
        }
        
    }
}