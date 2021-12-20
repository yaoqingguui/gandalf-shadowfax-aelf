using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Gandalf.Contracts.Shadowfax
{
    public partial class ShadowfaxContractState : ContractState
    {
        /// <summary>
        /// Admin
        /// </summary>
        public SingletonState<Address> Owner { get; set; }

        public Int64State MaximalTimeSpan { get; set; }
        public Int64State MinimalTimespan { get; set; }
        public MappedState<long, Address, UserInfo> UserInfo { get; set; }
        public MappedState<long, PublicOffering> PublicOfferingMap { get; set; }
        public Int64State CurrentPublicOfferingId { get; set; }
        public MappedState<string, Address> Ascription { get; set; }
    }
}