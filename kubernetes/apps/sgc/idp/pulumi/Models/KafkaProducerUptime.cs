using System.Collections.Immutable;

namespace authentik.Models;

public record KafkaProducerUptime : UptimeBase
{
  public override string Type { get; } = "kafka-producer";
  public string? KafkaProducerSaslOptionsMechanism { get; init; }
  public bool? KafkaProducerSsl { get; init; }
  public string KafkaProducerBrokers { get; init; }
  public string KafkaProducerTopic { get; init; }
  public string KafkaProducerMessage { get; init; }
  public ImmutableArray<string> AcceptedStatuscodes { get; init; } = ImmutableArray<string>.Empty;

}