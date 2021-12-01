using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace Gandalf.Contracts.IdoContract
{
    /// <summary>
    /// The C# implementation of the contract defined in ido_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class IdoContract : IdoContractContainer.IdoContractBase
    {
        public override Empty Initialize(Address input)
        {   
            Assert(State.Owner.Value==null,"Already initialized.");
            State.Owner.Value = input ?? Context.Sender;
            return new Empty();
        }
    }
}