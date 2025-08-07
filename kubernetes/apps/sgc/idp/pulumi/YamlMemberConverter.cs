using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dumpify;
using Humanizer;
using k8s.KubeConfigModels;
using YamlDotNet.Serialization;

namespace authentik;

public class YamlMemberConverterFactory : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
  {
    return typeToConvert.GetProperties().Any(z => z.GetCustomAttribute<YamlMemberAttribute>() != null);
  }

  public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    return (JsonConverter?)Activator.CreateInstance(typeof(YamlMemberConverter<>).MakeGenericType(typeToConvert));
  }

  class YamlMemberConverter<T> : JsonConverter<T> where T : new()
  {
    private static readonly FrozenDictionary<string, PropertyInfo> properties = typeof(T)
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.GetCustomAttribute<YamlMemberAttribute>() != null)
      .ToFrozenDictionary(p => p.GetCustomAttribute<YamlMemberAttribute>()!.Alias!, p => p);

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var config = new T();
      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject) break;
        if (reader.TokenType != JsonTokenType.PropertyName) continue;
        var propertyName = reader.GetString();
        if (propertyName is null || !properties.TryGetValue(propertyName, out var property)) continue;
        if (property.PropertyType == typeof(ImmutableList<string>))
        {
          reader.Read();
          if (reader.TokenType == JsonTokenType.StartArray && property.CanWrite)
          {
            var results = JsonSerializer.Deserialize<ImmutableList<string>>(ref reader, options);
            property.SetValue(config, results);
          }

          if (reader.TokenType == JsonTokenType.String && property.CanWrite)
          {
            property.SetValue(config,
              reader.GetString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToImmutableList());
          }

          continue;
        }

        if (property.PropertyType == typeof(ImmutableArray<string>))
        {
          reader.Read();
          if (reader.TokenType == JsonTokenType.StartArray && property.CanWrite)
          {
            var results = JsonSerializer.Deserialize<ImmutableArray<string>>(ref reader, options);
            property.SetValue(config, results);
          }

          if (reader.TokenType == JsonTokenType.String && property.CanWrite)
          {
            property.SetValue(config,
              reader.GetString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToImmutableArray());
          }

          continue;
        }

        if (property.CanWrite)
        property.SetValue(config, JsonSerializer.Deserialize(ref reader, property.PropertyType, options));
      }

      return config;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      foreach (var property in properties.Values)
      {
        if (property.GetValue(value) is not { } propertyValue) continue;
        var propertyName = properties.Single(z => z.Value == property).Key;
        writer.WritePropertyName(propertyName);
        if (value is IEnumerable<string> v)
        {
          writer.WriteStringValue(string.Join(",", v));
        }
        else
        {
          JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
        }
      }

      writer.WriteEndObject();
    }
  }
}
