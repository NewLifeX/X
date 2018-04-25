using System;
using System.Diagnostics.Contracts;

namespace NewLife.Net.MQTT.Packets
{
    /// <summary>包类型</summary>
    public class SubscriptionRequest : IEquatable<SubscriptionRequest>
    {
        public SubscriptionRequest(String topicFilter, QualityOfService qualityOfService)
        {
            Contract.Requires(!String.IsNullOrEmpty(topicFilter));

            TopicFilter = topicFilter;
            QualityOfService = qualityOfService;
        }

        public String TopicFilter { get; }

        public QualityOfService QualityOfService { get; }

        public Boolean Equals(SubscriptionRequest other)
        {
            return QualityOfService == other.QualityOfService
                && TopicFilter.Equals(other.TopicFilter, StringComparison.Ordinal);
        }

        public override String ToString() => $"{GetType().Name}[TopicFilter={TopicFilter}, QualityOfService={QualityOfService}]";
    }
}