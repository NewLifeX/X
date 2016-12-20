using System;
using NewLife.Queue.Utilities;

namespace NewLife.Queue.Protocols.Brokers
{
    [Serializable]
    public class BrokerInfo
    {
        /// <summary>Broker的名字，默认为DefaultBroker
        /// </summary>
        public string BrokerName { get; set; }
        /// <summary>Broker的分组名，当实现主备时，MasterBroker和它的所有的SlaveBroker的分组名相同；不同的MasterBroker的分组名要求不同；默认为DefaultGroup
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>Broker的集群名，一个集群下有可以有多个MasterBroker，每个MasterBroker可以有多个SlaveBroker；默认为DefaultCluster
        /// </summary>
        public string ClusterName { get; set; }
        /// <summary>Broker的角色，目前有Master,Slave两种角色；默认为Master
        /// </summary>
        public int BrokerRole { get; set; }
        /// <summary>供Producer连接的地址；默认IP为本地IP，端口为5000，格式为ip:port
        /// </summary>
        public string ProducerAddress { get; set; }
        /// <summary>供Consumer连接的地址；默认IP为本地IP，端口为5001，格式为ip:port
        /// </summary>
        public string ConsumerAddress { get; set; }
        /// <summary>Producer，Consumer对Broker发送的发消息和拉消息除外的其他内部请求，以及后台管理控制台发送的查询请求使用的地址；默认IP为本地IP，端口为5002，格式为ip:port
        /// </summary>
        public string AdminAddress { get; set; }

        public BrokerInfo() { }
        public BrokerInfo(string name, string groupName, string clusterName, BrokerRole role, string producerAddress, string consumerAddress, string adminAddress)
        {
            BrokerName = name;
            GroupName = groupName;
            ClusterName = clusterName;
            BrokerRole = (int)role;
            ProducerAddress = producerAddress;
            ConsumerAddress = consumerAddress;
            AdminAddress = adminAddress;
        }

        public void Valid()
        {
            Ensure.NotNullOrEmpty(BrokerName, "BrokerName");
            Ensure.NotNullOrEmpty(GroupName, "GroupName");
            Ensure.NotNullOrEmpty(ClusterName, "ClusterName");
            Ensure.NotNull(ProducerAddress, "ProducerAddress");
            Ensure.NotNull(ConsumerAddress, "ConsumerAddress");
            Ensure.NotNull(AdminAddress, "AdminAddress");
            if (BrokerRole != (int)Brokers.BrokerRole.Master && BrokerRole != (int)Brokers.BrokerRole.Slave)
            {
                throw new ArgumentException("Invalid broker role: " + BrokerRole);
            }
        }
        public bool IsEqualsWith(BrokerInfo other)
        {
            if (other == null)
            {
                return false;
            }

            return BrokerName == other.BrokerName
                && GroupName == other.GroupName
                && ClusterName == other.ClusterName
                && BrokerRole == other.BrokerRole
                && ProducerAddress == other.ProducerAddress
                && ConsumerAddress == other.ConsumerAddress
                && AdminAddress == other.AdminAddress;
        }

        public override string ToString()
        {
            return string.Format("[BrokerName={0},GroupName={1},ClusterName={2},BrokerRole={3},ProducerAddress={4},ConsumerAddress={5},AdminAddress={6}]",
                BrokerName,
                GroupName,
                ClusterName,
                BrokerRole,
                ProducerAddress,
                ConsumerAddress,
                AdminAddress);
        }
    }
}
