using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.Shadowfax
{
    public partial class ShadowfaxContract
    {
        public override Address GetOwner(Empty input)
        {
            return State.Owner.Value;
        }
        
        
        public override ResetTimeSpanOutput GetTimespan(Empty input)
        {
            return new ResetTimeSpanOutput
            {
                MaxTimespan = State.MaximalTimeSpan.Value,
                MinTimespan = State.MinimalTimespan.Value
            };
        }

        public override PublicOfferingOutput PublicOfferings(Int32Value input)
        {
            var offering = State.PublicOfferList.Value.Value[input.Value];
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
            if (userInfo!=null)
            {
                return userInfo;
            }
            return new UserInfoStruct
            {
                Claimed = false,
                ObtainAmount = 0
            };
        }
        
        public override Int32Value GetPublicOfferingLength(Empty input)
        {
            return new Int32Value
            {
                Value = State.PublicOfferList.Value.Value.Count
            };
        }
        
        public override Address GetTokenOwnership(Token input)
        {
            return State.Ascription[input.TokenSymbol];
        }
       
    }
    
}