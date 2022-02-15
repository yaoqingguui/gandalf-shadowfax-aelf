using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Shadowfax
{
    public partial class ShadowfaxContract
    {
        public override Int64Value AddPublicOffering(AddPublicOfferingInput input)
        {
            AssertContractInitialized();

            Assert(input.OfferingTokenAmount > 0, "Need deposit some offering token.");
            Assert(input.StartTime >= Context.CurrentBlockTime, "Invalid start time.");
            Assert(input.EndTime.Seconds <= input.StartTime.Seconds + State.MaximalTimeSpan.Value &&
                   input.EndTime.Seconds >= input.StartTime.Seconds + State.MinimalTimespan.Value, "Invalid end time.");
            var owner = State.AscriptionMap[input.OfferingTokenSymbol];
            if (owner != null)
            {
                Assert(owner == Context.Sender, "Another has published the token before.");
            }
            else
            {
                State.AscriptionMap[input.OfferingTokenSymbol] = Context.Sender;
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
            };
            State.PublicOfferingMap[publicId] = offering;

            State.CurrentPublicOfferingId.Value = publicId.Add(1);

            Context.Fire(new AddPublicOffering
            {
                OfferingTokenSymbol = offering.OfferingTokenSymbol,
                OfferingTokenAmount = offering.OfferingTokenAmount,
                WantTokenSymbol = offering.WantTokenSymbol,
                WantTokenAmount = offering.WantTokenAmount,
                Publisher = Context.Sender,
                StartTime = offering.StartTime,
                EndTime = offering.EndTime,
                PublicId = publicId
            });

            return new Int64Value
            {
                Value = publicId
            };
        }

        public override Empty ChangeAscription(ChangeAscriptionInput input)
        {
            AssertContractInitialized();

            Assert(State.AscriptionMap[input.TokenSymbol] == Context.Sender || State.Owner.Value == Context.Sender,
                "No permission.");
            // test

            var stateAscription = State.AscriptionMap[input.TokenSymbol];
            var contextSender = Context.Sender;
            var ownerValue = State.Owner.Value;
            // test
            State.AscriptionMap[input.TokenSymbol] = input.Receiver;
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
            AssertContractInitialized();

            Assert(input.Value >= 0, "Invalid number.");
            var offering = GetOffering(input.Value);
            Assert(!offering.Claimed, "Have withdrawn.");
            Assert(offering.Publisher == Context.Sender, "No permission.");
            Assert(Context.CurrentBlockTime > offering.EndTime, "Game not over.");
            offering.Claimed = true;
            var surplus = offering.OfferingTokenAmount.Sub(offering.SubscribedOfferingAmount);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Amount = offering.WantTokenBalance,
                Symbol = offering.WantTokenSymbol
            });

            if (surplus > 0)
            {
                State.TokenContract.Transfer.Send(new TransferInput
                {
                    Amount = surplus,
                    Symbol = offering.OfferingTokenSymbol,
                    To = Context.Sender,
                });
            }

            Context.Fire(new Withdraw
            {
                PubilicId = input.Value,
                To = Context.Sender,
                OfferingToken = surplus,
                WantToken = offering.WantTokenBalance
            });
            return new Empty();
        }
    }
}