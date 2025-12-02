using Baubit.Reflection;
using Baubit.Traceability;
using FluentResults;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Baubit.DI
{
    public static class ModuleExtensions
    {

        public static Result<string> Serialize<TModule>(this TModule module,
                                                        JsonSerializerOptions jsonSerializerOptions) where TModule : IModule
        {
            return Result.Try(() =>
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = jsonSerializerOptions?.WriteIndented == true }))
                    {
                        return module.Serialize(writer, jsonSerializerOptions)
                                     .Bind(w => Result.Try(() =>
                                     {
                                         w.Flush();
                                         return Encoding.UTF8.GetString(stream.ToArray());
                                     }))
                                     .ThrowIfFailed()
                                     .Value;
                    }
                }
            });
        }

        public static Result<string> SerializeAsJsonObject(this IEnumerable<IModule> modules,
                                                            JsonSerializerOptions jsonSerializerOptions)
        {
            return Result.Try(() =>
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = jsonSerializerOptions?.WriteIndented == true }))
                    {
                        return Result.Try(() => writer.WriteStartObject())
                                     .Bind(() => modules.WriteModules(writer, jsonSerializerOptions))
                                     .Bind(_ => Result.Try(() => writer.WriteEndObject()))
                                     .Bind(() => Result.Try(() =>
                                     {
                                         writer.Flush();
                                         return Encoding.UTF8.GetString(stream.ToArray());
                                     }))
                                     .ThrowIfFailed()
                                     .Value;
                    }
                }
            });
        }

        private static Result<Utf8JsonWriter> Serialize<TModule>(this TModule module,
                                                                 Utf8JsonWriter writer,
                                                                 JsonSerializerOptions jsonSerializerOptions) where TModule : IModule
        {
            return Result.Try(() =>
            {
                writer.WriteStartObject();

                module.WriteModuleDescriptor(writer, jsonSerializerOptions);

                writer.WriteEndObject();
                return writer;
            });
        }

        private static Result<Utf8JsonWriter> WriteModuleDescriptor<TModule>(this TModule module,
                                                                             Utf8JsonWriter writer,
                                                                             JsonSerializerOptions jsonSerializerOptions) where TModule : IModule
        {
            return Result.Try(() =>
            {
                writer.WriteString("type", module.GetType().GetBaubitFormattedAssemblyQualifiedName().ThrowIfFailed().Value);
                writer.WritePropertyName("configuration");
                using (var configJson = JsonDocument.Parse(JsonSerializer.Serialize(Convert.ChangeType(module.Configuration, module.Configuration.GetType()), jsonSerializerOptions)))
                {
                    writer.WriteStartObject();

                    foreach (var property in configJson.RootElement.EnumerateObject())
                    {
                        property.WriteTo(writer); // copy all properties as-is
                    }
                }

                module.NestedModules.WriteModules(writer, jsonSerializerOptions).ThrowIfFailed();

                writer.WriteEndObject();
                return writer;
            });
        }

        private static Result<Utf8JsonWriter> WriteModules(this IEnumerable<IModule> modules,
                                                           Utf8JsonWriter writer,
                                                           JsonSerializerOptions jsonSerializerOptions)
        {
            return Result.Try(() => writer.WritePropertyName("modules"))
                         .Bind(() => Result.Try(() => writer.WriteStartArray()))
                         .Bind(() => modules.Aggregate(Result.Ok(writer), (seed, next) => seed.Bind(w => next.Serialize(w, jsonSerializerOptions))))
                         .Bind(_ => Result.Try(() => writer.WriteEndArray()))
                         .Bind(() => Result.Ok(writer));
        }
    }
}
