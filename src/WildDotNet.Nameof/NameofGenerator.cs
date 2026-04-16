using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using WildDotNet.Nameof.Internal.Generation;
using WildDotNet.Nameof.Internal.Processing;
using WildDotNet.Nameof.Internal.Requests;
using WildDotNet.Nameof.Internal.Resolvers;
using WildDotNet.Nameof.Internal.Support;

namespace WildDotNet.Nameof;

[Generator]
internal sealed class NameofGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource($"{GeneratorConstants.HintPrefix}.Core.g.cs", SourceText.From(NameofCoreSource.BaseText, Encoding.UTF8));
        });

        var pipeline = context.CompilationProvider
            .Select(static (compilation, _) => WithAllMetadata(compilation))
            .Select(static (compilation, _) => (Compilation: compilation, Requests: NameofRequestParser.Parse(compilation)));

        context.RegisterSourceOutput(pipeline, static (spc, input) =>
        {
            ITypeMemberResolver[] resolvers =
            [
                new CurrentCompilationMemberResolver(),
                new ExternalReflectionMemberResolver()
            ];
            var processor = new NameofRequestProcessor(input.Compilation, resolvers);

            var genericSupportSource = NameofCoreSource.CreateGenericSupport(
                input.Requests
                    .Where(static request => request.Generic.IsOpenDefinition)
                    .Select(static request => request.Generic.Arity));

            if (!string.IsNullOrWhiteSpace(genericSupportSource))
            {
                spc.AddSource(
                    $"{GeneratorConstants.HintPrefix}.GenericSupport.g.cs",
                    SourceText.From(genericSupportSource, Encoding.UTF8));
            }

            foreach (var request in input.Requests)
            {
                var result = processor.Process(request);

                foreach (var diagnostic in result.Diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                }

                if (result.EmissionPlan is null)
                {
                    continue;
                }

                var source = NameofSourceEmitter.Render(result.EmissionPlan);
                if (!string.IsNullOrWhiteSpace(source))
                {
                    spc.AddSource(
                        $"{GeneratorConstants.HintPrefix}.{result.EmissionPlan.WrapperHintIdentity}.g.cs",
                        SourceText.From(source, Encoding.UTF8));
                }
            }
        });
    }

    private static Compilation WithAllMetadata(Compilation compilation)
    {
        if (compilation is not CSharpCompilation csharpCompilation)
        {
            return compilation;
        }

        var options = csharpCompilation.Options;
        if (options.MetadataImportOptions == MetadataImportOptions.All)
        {
            return compilation;
        }

        return csharpCompilation.WithOptions(options.WithMetadataImportOptions(MetadataImportOptions.All));
    }
}
