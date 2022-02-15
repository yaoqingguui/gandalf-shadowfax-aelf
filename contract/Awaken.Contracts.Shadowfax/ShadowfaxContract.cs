using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Shadowfax
{
    public partial class ShadowfaxContract : ShadowfaxContractContainer.ShadowfaxContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.Owner.Value == null, "Already initialized.");
            State.Owner.Value = input.Owner ?? Context.Sender;
            Context.LogDebug(() => State.Owner.Value.ToString());

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }

        private void AssertContractInitialized()
        {
            Assert(State.Owner.Value != null, "Contract not initialized.");
        }
    }
}