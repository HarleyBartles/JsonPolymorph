﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace JsonPolymorph
{
    [Generator]
    internal sealed class JsonHierarchySourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Retrieve the populated receiver.
            if (!(context.SyntaxContextReceiver is DecoratedRecordHierarchyIdentifier receiver))
                return;

            // Add the JSON inheritance converter attribute to all hierarchy base record types,
            // along with a KnownType attribute for each record type deriving from the base type.
            foreach (var hierarchyBaseRecordType in receiver.DecoratedRecordTypes)
            {
                var fileName = $"{hierarchyBaseRecordType.Namespace}.{hierarchyBaseRecordType.RecordType}.cs";
                var codeBuilder = new StringBuilder();
                codeBuilder.AppendLine($@"/* Source file generated by {nameof(JsonPolymorph)} */
using Newtonsoft.Json;
using NJsonSchema.Converters;
using System.Runtime.Serialization;

namespace {hierarchyBaseRecordType.Namespace}
{{
    [JsonConverter(typeof(JsonInheritanceConverter), ""discriminator"")]");

                foreach (var x in receiver.PotentialDerivedRecordTypes.Where(typeHierarchy =>
                    typeHierarchy.Last() == hierarchyBaseRecordType).OrderBy(typeHierarchy => typeHierarchy.First()))
                {
                    var derivedRecordType = x.First();
                    codeBuilder.AppendLine(
                        $@"    [KnownType(typeof({derivedRecordType.Namespace}.{derivedRecordType.RecordType}))]");
                }

                codeBuilder.AppendLine($@"    public abstract partial record {hierarchyBaseRecordType.RecordType};
}}");
                context.AddSource(fileName, SourceText.From(codeBuilder.ToString(), Encoding.UTF8));
            }

#if DEBUG
            //Write the log entries for debugging.
            var newline = System.Environment.NewLine;
            context.AddSource("Logs", SourceText.From(
                $@"/*{ newline + string.Join(newline, receiver.Log) + newline}*/", Encoding.UTF8));
#endif
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new DecoratedRecordHierarchyIdentifier(
                decoratingAttributeClassName: nameof(JsonHierarchyBaseAttribute),
                decoratingAttributeFullNamespace: nameof(JsonPolymorph)));
        }
    }
}
