using AElf;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.Shadowfax
{
    public partial class ShadowfaxContract : ShadowfaxContractContainer.ShadowfaxContractBase
    {
        public override Empty Initialize(Address input)
        {
            Assert(State.Owner.Value == null, "Already initialized.");
            State.Owner.Value = input == null || input.Value.IsNullOrEmpty() ? Context.Sender : input;
            Context.LogDebug(()=>State.Owner.Value.ToString());
            State.PublicOfferList.Value = new PublicOfferList();
            
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }
    }
}