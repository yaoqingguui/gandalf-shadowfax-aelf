using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace IdoContract
{
    /// <summary>
    /// The state class of the contract, it inherits from the AElf.Sdk.CSharp.State.ContractState type. 
    /// </summary>
    public partial class IdoContractState : ContractState
    {
        // state definitions go here.
        public Int64State MaximalTimeSpan { get; set; }
        public Int64State MinimalTimespan { get; set; }
        public SingletonState<Address> WETH;
        public MappedState<Int64State,Address,UserInfo> UserInfo { get; set; }
        public SingletonState<PublicOfferList> PublicOfferList { get; set; }
        public MappedState<Address, Address> Ascription;
    }
}