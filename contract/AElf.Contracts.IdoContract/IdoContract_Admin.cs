using Gandalf.Contracts.IdoContract;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.IdoContract
{
    public partial class IdoContract
    {
        public override Empty ResetTimeSpan(ResetTimeSpanInput input)
        {
            AssertSenderIsOwner();
            Assert(input.MaxTimespan > input.MinTimespan, "Invalid parameter.");
            State.MaximalTimeSpan.Value = input.MaxTimespan;
            State.MinimalTimespan.Value = input.MinTimespan;
            return new Empty();
        }

        private void AssertSenderIsOwner()
        {
            Assert(State.Owner.Value != null, "Contract not initialized.");
            Assert(Context.Sender == State.Owner.Value);
        }
    }
}