using Google.Protobuf.WellKnownTypes;

namespace Awaken.Contracts.Shadowfax
{
    public partial class ShadowfaxContract
    {
        public override Empty ResetTimeSpan(ResetTimeSpanInput input)
        {
            AssertSenderIsOwner();
            AssertContractInitialized();
            Assert(input.MaxTimespan > input.MinTimespan, "Invalid parameter.");
            State.MaximalTimeSpan.Value = input.MaxTimespan;
            State.MinimalTimespan.Value = input.MinTimespan;
            return new Empty();
        }

        private void AssertSenderIsOwner()
        {
            AssertContractInitialized();
            Assert(Context.Sender == State.Owner.Value,"Not Owner.");
        }
    }
}