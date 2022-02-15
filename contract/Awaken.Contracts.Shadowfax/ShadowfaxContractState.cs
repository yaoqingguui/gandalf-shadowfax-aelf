using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace Awaken.Contracts.Shadowfax
{
    public partial class ShadowfaxContractState : ContractState
    {
        /// <summary>
        /// aka Admin
        /// </summary>
        public SingletonState<Address> Owner { get; set; }

        public Int64State MaximalTimeSpan { get; set; }
        public Int64State MinimalTimespan { get; set; }

        public Int64State CurrentPublicOfferingId { get; set; }

        /// <summary>
        /// Public Offering Id -> User Address -> User Information
        /// </summary>
        public MappedState<long, Address, UserInfoStruct> UserInfoMap { get; set; }

        /// <summary>
        /// Public Offering Id -> Public Offering Information
        /// </summary>
        public MappedState<long, PublicOffering> PublicOfferingMap { get; set; }

        /// <summary>
        /// IDO Token Symbol -> Ascription Address
        /// </summary>
        public MappedState<string, Address> AscriptionMap { get; set; }
    }
}