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
    /// <summary>
    /// Extension methods for serializing modules to JSON format.
    /// </summary>
    public static class ModuleExtensions
    {
        /// <summary>
        /// Serializes a module to a JSON string representation.
        /// </summary>
        /// <typeparam name="TModule">The type of module to serialize.</typeparam>
        /// <param name="module">The module to serialize.</param>
        /// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
        /// <returns>A result containing the JSON string, or failure information.</returns>
        /// <remarks>
        /// The output includes the module's type, configuration, and nested modules in a format
        /// that can be used to recreate the module hierarchy.
        /// </remarks>
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

        /// <summary>
        /// Serializes a collection of modules to a JSON object with a "modules" array property.
        /// </summary>
        /// <param name="modules">The modules to serialize.</param>
        /// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
        /// <returns>A result containing the JSON string, or failure information.</returns>
        /// <remarks>
        /// The output format is: <c>{ "modules": [ ... ] }</c> where each module includes
        /// its type, configuration, and nested modules.
        /// </remarks>
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
