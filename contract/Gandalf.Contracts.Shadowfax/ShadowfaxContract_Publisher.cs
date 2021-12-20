using System.Numerics;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.Shadowfax
{
    public partial class ShadowfaxContract
    {
        public override Int64Value AddPublicOffering(AddPublicOfferingInput input)
        {
            Assert(input.OfferingTokenAmount > 0, "Need deposit some offering token.");
            Assert(input.StartTime >= Context.CurrentBlockTime, "Invalid start time.");
            Assert(input.EndTime.Seconds <= input.StartTime.Seconds + State.MaximalTimeSpan.Value &&
                   input.EndTime.Seconds >= input.StartTime.Seconds + State.MinimalTimespan.Value, "Invalid end time.");
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
                Amount = input.OfferingTokenAmount,
                Symbol = input.OfferingTokenSymbol
            });

            var publicId = State.CurrentPublicOfferingId.Value;
            var offering = new PublicOffering
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
            };
            State.PublicOfferingMap[publicId] = offering;

            State.CurrentPublicOfferingId.Value = publicId.Add(1);
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

        public override Empty Withdraw(Int64Value input)
        {
            Assert(input.Value >= 0, "Invalid number.");
            var offering = GetOffering(input.Value);
            Assert(!offering.Claimed, "Have withdrawn.");
            Assert(offering.Publisher == Context.Sender, "No rights.");
            Assert(Context.CurrentBlockTime > offering.EndTime, "Game not over.");
            offering.Claimed = true;
            BigInteger wantTokenBalance = offering.WantTokenBalance;
            BigInteger surplus = offering.OfferingTokenAmount.Sub(offering.SubscribedOfferingAmount);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Amount = (long) wantTokenBalance,
                Symbol = offering.WantTokenSymbol
            });

            if (surplus > 0)
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    Amount = (long) surplus,
                    Symbol = offering.OfferingTokenSymbol,
                    To = Context.Sender,
                });
            }

            Context.Fire(new Withdraw
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