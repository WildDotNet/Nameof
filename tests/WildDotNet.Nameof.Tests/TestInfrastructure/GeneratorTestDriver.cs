using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WildDotNet.Nameof.Tests.TestInfrastructure;

internal static class GeneratorTestDriver
{
    public static GeneratorRunResult Run(string source, params MetadataReference[] extraReferences)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references: GetDefaultReferences().AddRange(extraReferences),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = CreateGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.Results.Length == 0
            ? ImmutableArray<GeneratedSourceResult>.Empty
            : runResult.Results[0].GeneratedSources
                .Select(static source => new GeneratedSourceResult(
                    source.HintName,
                    NormalizeLineEndings(source.SourceText.ToString())))
                .ToImmutableArray();

        using var emitStream = new MemoryStream();
        var emitResult = outputCompilation.Emit(emitStream);
        Assert.True(
            emitResult.Success,
            string.Join(
                Environment.NewLine,
                emitResult.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        return new GeneratorRunResult(
            diagnostics.AddRange(outputCompilation.GetDiagnostics()).AddRange(emitResult.Diagnostics),
            generatedSources);
    }

    private static IIncrementalGenerator CreateGenerator()
    {
        var assembly = LoadGeneratorAssembly();
        var generatorType = assembly.DefinedTypes.SingleOrDefault(static type =>
                type.Name == "NameofGenerator" &&
                typeof(IIncrementalGenerator).IsAssignableFrom(type))
            ?? throw new InvalidOperationException(
                $"Could not find the internal generator type in '{assembly.Location}'.");

        var instance = Activator.CreateInstance(generatorType.AsType(), nonPublic: true);

        if (instance is not IIncrementalGenerator generator)
        {
            throw new InvalidOperationException(
                $"The generator type '{generatorType.FullName}' in '{assembly.Location}' does not implement IIncrementalGenerator.");
        }

        return generator;
    }

    private static Assembly LoadGeneratorAssembly()
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "WildDotNet.Nameof.dll");
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"Could not locate the Nameof project-reference assembly in the test output directory. Expected a file at '{assemblyPath}'.");
        }

        return Assembly.LoadFrom(assemblyPath);
    }

    private static ImmutableArray<MetadataReference> GetDefaultReferences()
    {
        var builder = ImmutableArray.CreateBuilder<MetadataReference>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                AddReference(path);
            }
        }

        AddReference(Path.Combine(AppContext.BaseDirectory, "WildDotNet.Nameof.dll"));

        return builder.ToImmutable();

        void AddReference(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                return;
            }

            var fullPath = Path.GetFullPath(path);
            if (!seenPaths.Add(fullPath))
            {
                return;
            }

            builder.Add(MetadataReference.CreateFromFile(fullPath));
        }
    }

    internal static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

internal sealed record GeneratorRunResult(
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<GeneratedSourceResult> GeneratedSources)
{
    public GeneratorRunSnapshot ToSnapshot()
    {
        var diagnostics = Diagnostics
            .Where(static diagnostic => diagnostic.Id.StartsWith("NAMEOF", StringComparison.Ordinal))
            .OrderBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.GetMessage(), StringComparer.Ordinal)
            .Select(static diagnostic => new DiagnosticSnapshot(
                diagnostic.Id,
                diagnostic.Severity.ToString(),
                GeneratorTestDriver.NormalizeLineEndings(diagnostic.GetMessage())))
            .ToImmutableArray();

        var generatedSources = GeneratedSources
            .OrderBy(static generatedSource => generatedSource.HintName, StringComparer.Ordinal)
            .Select(static generatedSource => new GeneratedSourceSnapshot(
                generatedSource.HintName,
                generatedSource.Source))
            .ToImmutableArray();

        return new GeneratorRunSnapshot(diagnostics, generatedSources);
    }
}

internal sealed record GeneratedSourceResult(
    string HintName,
    string Source);

internal sealed record GeneratorRunSnapshot(
    ImmutableArray<DiagnosticSnapshot> Diagnostics,
    ImmutableArray<GeneratedSourceSnapshot> GeneratedSources);

internal sealed record DiagnosticSnapshot(
    string Id,
    string Severity,
    string Message);

internal sealed record GeneratedSourceSnapshot(
    string HintName,
    string Source);
