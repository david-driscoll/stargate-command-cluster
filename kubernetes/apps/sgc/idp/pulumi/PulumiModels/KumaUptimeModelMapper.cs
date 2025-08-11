using System;
using Models.ApplicationDefinition;
using Models.UptimeKuma;
using Riok.Mapperly.Abstractions;

namespace Models;

[Mapper(AllowNullPropertyAssignment = false)]
static partial class KumaUptimeModelMapper
{
  public static UptimeBase GetUptime(ApplicationDefinitionUptime uptime)
  {
    return uptime switch
    {
      { Http: not null } => uptime.Http,
      { Ping: not null } => uptime.Ping,
      { Docker: not null } => uptime.Docker,
      { Dns: not null } => uptime.Dns,
      { Gamedig: not null } => uptime.Gamedig,
      { Group: not null } => uptime.Group,
      { GrpcKeyword: not null } => uptime.GrpcKeyword,
      { JsonQuery: not null } => uptime.JsonQuery,
      { KafkaProducer: not null } => uptime.KafkaProducer,
      { Keyword: not null } => uptime.Keyword,
      { MongoDb: not null } => uptime.MongoDb,
      { Mqtt: not null } => uptime.Mqtt,
      { Mysql: not null } => uptime.Mysql,
      { Port: not null } => uptime.Port,
      { Postgres: not null } => uptime.Postgres,
      { Push: not null } => uptime.Push,
      { Radius: not null } => uptime.Radius,
      { RealBrowser: not null } => uptime.RealBrowser,
      { Redis: not null } => uptime.Redis,
      { Steam: not null } => uptime.Steam,
      { SqlServer: not null } => uptime.SqlServer,
      { TailscalePing: not null } => uptime.TailscalePing,
      _ => throw new ArgumentOutOfRangeException(nameof(uptime), "Unknown Uptime type")
    };
  }

  public static void MapUptime([MappingTarget] KumaUptimeResourceConfigArgs args,
    ApplicationDefinitionUptime definition)
  {
    switch (definition)
    {
      case { Http: not null }:
        MapToUptime(args, definition.Http);
        return;
      case { Ping: not null }:
        MapToUptime(args, definition.Ping);
        return;
      case { Docker: not null }:
        MapToUptime(args, definition.Docker);
        return;
      case { Dns: not null }:
        MapToUptime(args, definition.Dns);
        return;
      case { Gamedig: not null }:
        MapToUptime(args, definition.Gamedig);
        return;
      case { Group: not null }:
        MapToUptime(args, definition.Group);
        return;
      case { GrpcKeyword: not null }:
        MapToUptime(args, definition.GrpcKeyword);
        return;
      case { JsonQuery: not null }:
        MapToUptime(args, definition.JsonQuery);
        return;
      case { KafkaProducer: not null }:
        MapToUptime(args, definition.KafkaProducer);
        return;
      case { Keyword: not null }:
        MapToUptime(args, definition.Keyword);
        return;
      case { MongoDb: not null }:
        MapToUptime(args, definition.MongoDb);
        return;
      case { Mqtt: not null }:
        MapToUptime(args, definition.Mqtt);
        return;
      case { Mysql: not null }:
        MapToUptime(args, definition.Mysql);
        return;
      case { Port: not null }:
        MapToUptime(args, definition.Port);
        return;
      case { Postgres: not null }:
        MapToUptime(args, definition.Postgres);
        return;
      case { Push: not null }:
        MapToUptime(args, definition.Push);
        return;
      case { Radius: not null }:
        MapToUptime(args, definition.Radius);
        return;
      case { RealBrowser: not null }:
        MapToUptime(args, definition.RealBrowser);
        return;
      case { Redis: not null }:
        MapToUptime(args, definition.Redis);
        return;
      case { Steam: not null }:
        MapToUptime(args, definition.Steam);
        return;
      case { SqlServer: not null }:
        MapToUptime(args, definition.SqlServer);
        return;
      case { TailscalePing: not null }:
        MapToUptime(args, definition.TailscalePing);
        return;
      default:
        throw new ArgumentOutOfRangeException(nameof(definition), $"Unknown Uptime type in definition");
    }
  }

  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, DnsUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, HttpUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PingUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, DockerUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GamedigUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GroupUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, GrpcKeywordUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, JsonQueryUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, KafkaProducerUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, KeywordUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MongoDbUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MqttUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, MysqlUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PortUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PostgresUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, PushUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RadiusUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RealBrowserUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, RedisUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, SteamUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, SqlServerUptime uptime);


  public static partial void MapToUptime([MappingTarget] KumaUptimeResourceConfigArgs args, TailscalePingUptime uptime);
}
