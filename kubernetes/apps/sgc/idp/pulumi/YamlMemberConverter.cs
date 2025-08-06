using System;
using System.Collections.Frozen;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        if (reader.TokenType != JsonTokenType.PropertyName) continue;
        var propertyName = reader.GetString();
        if (properties.TryGetValue(propertyName, out var property))
        {
          property.SetValue(config, JsonSerializer.Deserialize(ref reader, property.PropertyType, options));
        }
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
        JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
      }

      writer.WriteEndObject();
    }
  }
}
