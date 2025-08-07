using System.Collections.Immutable;

namespace authentik.Models;

public class KafkaProducerUptime : UptimeBase
{
  public override string Type { get; } = "kafka-producer";
  [YamlDotNet.Serialization.YamlMember(Alias = "kafka_producer_sasl_options_mechanism")]
  [System.Text.Json.Serialization.JsonPropertyName("kafka_producer_sasl_options_mechanism")]
  public string? KafkaProducerSaslOptionsMechanism { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "kafka_producer_ssl")]
  [System.Text.Json.Serialization.JsonPropertyName("kafka_producer_ssl")]
  public bool? KafkaProducerSsl { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "kafka_producer_brokers")]
  [System.Text.Json.Serialization.JsonPropertyName("kafka_producer_brokers")]
  public string KafkaProducerBrokers { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "kafka_producer_topic")]
  [System.Text.Json.Serialization.JsonPropertyName("kafka_producer_topic")]
  public string KafkaProducerTopic { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "kafka_producer_message")]
  [System.Text.Json.Serialization.JsonPropertyName("kafka_producer_message")]
  public string KafkaProducerMessage { get; set; }
  [YamlDotNet.Serialization.YamlMember(Alias = "accepted_statuscodes")]
  [System.Text.Json.Serialization.JsonPropertyName("accepted_statuscodes")]
  public ImmutableList<string>? AcceptedStatusCodes { get; set; }

}
