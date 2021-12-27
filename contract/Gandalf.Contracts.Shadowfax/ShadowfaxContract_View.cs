using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.Shadowfax
{
    public partial class ShadowfaxContract
    {
        public override Address Owner(Empty input)
        {
            return State.Owner.Value;
        }

        public override Int64Value MaximalTimeSpan(Empty input)
        {
            return new Int64Value
            {
                Value = State.MaximalTimeSpan.Value
            };
        }
    
        public override Int64Value MinimalTimespan(Empty input)
        {
            return new Int64Value
            {
                Value = State.MinimalTimespan.Value
            };
        }

        public override PublicOfferingOutput PublicOfferings(Int64Value input)
        {
            var offering = State.PublicOfferingMap[input.Value];
            if (offering == null)
            {
                return new PublicOfferingOutput();
            }

            return new PublicOfferingOutput
            {
                Claimed = offering.Claimed,
                Publisher = offering.Publisher,
                EndTime = offering.EndTime,
                StartTime = offering.StartTime,
                PublicId = input.Value,
                OfferingTokenAmount = offering.OfferingTokenAmount,
                OfferingTokenSymbol = offering.OfferingTokenSymbol,
                SubscribedOfferingAmount = offering.SubscribedOfferingAmount,
                WantTokenAmount = offering.WantTokenAmount,
                WantTokenSymbol = offering.WantTokenSymbol,
                WantTokenBalance = offering.WantTokenBalance
            };
        }

        public override UserInfoStruct UserInfo(UserInfoInput input)
        {
            var userInfo = State.UserInfo[input.PublicId][input.User];
            if (userInfo != null)
            {
                return userInfo;
            }

            return new UserInfoStruct
            {
                Claimed = false,
                ObtainAmount = 0
            };
        }

        public override Int64Value GetPublicOfferingLength(Empty input)
        {
            return new Int64Value
            {
                Value = State.CurrentPublicOfferingId.Value
            };
        }


        public override Address Ascription(StringValue input)
        {
            return State.Ascription[input.Value];
        }
    }
}