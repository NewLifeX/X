
namespace NewLife.Net.Sip.Message
{
    /// <summary></summary>
    public class SipAddressParam : SipValueWithParams
    {
        #region 属性
        private SipNameAddress _Address;
        /// <summary>地址</summary>
        public SipNameAddress Address { get { return _Address; } set { _Address = value; } }
        #endregion
    }
}