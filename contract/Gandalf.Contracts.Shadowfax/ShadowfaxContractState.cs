using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Gandalf.Contracts.Shadowfax
{
    public partial class ShadowfaxContractState : ContractState
    {
        // Admin
        public SingletonState<Address> Owner { get; set; }
        // state definitions go here.
        public Int64State MaximalTimeSpan { get; set; }
        public Int64State MinimalTimespan { get; set; }
        public MappedState<long,Address,UserInfoStruct> UserInfo { get; set; }
        public SingletonState<PublicOfferList> PublicOfferList { get; set; }
        public MappedState<string, Address> Ascription { get; set; } 
    }
}