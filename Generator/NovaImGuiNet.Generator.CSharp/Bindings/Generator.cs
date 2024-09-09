using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public class Generator {
    private static readonly ILogger Log = Logging.Create<Generator>();

    private readonly string _rootOutputPath;
    private readonly BindingsGenerationOptions _bindingsGenerationOptions;

    private BindingsContext BindingsContext { get; set; }

    public Generator(string rootOutputPath, BindingsGenerationOptions bindingsGenerationOptions) {
        _rootOutputPath = rootOutputPath;
        _bindingsGenerationOptions = bindingsGenerationOptions;
    }

    public void Execute() {
        Stopwatch totalStopwatch = Stopwatch.StartNew();

        MetadataDto metadataDto;
        using (new TimedLoggedOperation(Log, "Parsing metadata")) {
            string[] loadedFiles = [
                "Resources/cimgui/cimgui_impl_sdl2.json",
                "Resources/cimgui/cimgui_impl_sdlrenderer2.json",
                "Resources/cimgui/cimgui_impl_win32.json",
                "Resources/cimgui/cimgui_imconfig.json",
                "Resources/cimgui/cimgui.json",
                "Resources/cimgui/cimgui_internal.json",
                "Resources/cimgui/cimgui_internal_imstb_textedit_minimal.json",

                //"Resources/cimgui/cimplot.json",
            ];

            Task<MetadataDto>[] tasks =
                loadedFiles
                    .Select(x => Task.Run(() => LoadDearBindingsDto(x)))
                    .ToArray();

            Task.WhenAll(tasks).Wait();

            metadataDto = MergeDearBindingsDtos(tasks.Select(x => x.Result));

            // HACK: add definition for FILE
            metadataDto.Typedefs.Add(new MetadataDto.Typedef {
                Name = "FILE",
                Type = new MetadataDto.TypeValue {
                    Declaration = "void*",
                    Description = new MetadataDto.TypeValue.TypeDescriptionPointer {
                        InnerType = new MetadataDto.TypeValue.TypeDescriptionBuiltin {
                            BuiltinType = "void"
                        }
                    }
                },
                SourceLocation = new MetadataDto.SourceLocation("imgui.h", 0)
            });
        }

        MetadataModel metadataModel;
        using (new TimedLoggedOperation(Log, "Resolving metadata model")) {
            metadataModel = new MetadataModelResolver(metadataDto).Map();
        }

        BindingsModel bindingsModel;
        using (new TimedLoggedOperation(Log, "Resolving bindings model")) {
            bindingsModel = ResolveBindingsModel(metadataModel);
        }

        using (new TimedLoggedOperation(Log, "Parsing bindings customizations")) {
            BindingsCustomizationsContext bindingsCustomizationsContext = LoadBindingsCustomizationsContext();
            BindingsContext = new BindingsContext(
                _rootOutputPath,
                "NovaImGuiNet",
                bindingsCustomizationsContext,
                _bindingsGenerationOptions
            );
        }

        using (new TimedLoggedOperation(Log, "Applying transformations")) {
            bindingsModel =
                new BindingsModelCSharpTransformer(
                    bindingsModel,
                    metadataDto,
                    BindingsContext
                ).Transform();
        }

        IReadOnlyList<BindingsCompilationUnit> compilationUnits;
        using (new TimedLoggedOperation(Log, "Creating C# entities")) {
            BindingsModelCodeGenerator codeGenerator = new(bindingsModel, BindingsContext);
            compilationUnits = codeGenerator.Generate();
        }

        if (!Directory.Exists(BindingsContext.RootOutputPath))
            throw new DirectoryNotFoundException($"Directory '{BindingsContext.RootOutputPath}' doesn't exist");

        using (new TimedLoggedOperation(Log, "Cleaning output directories")) {
            new DirectoryInfo(BindingsContext.RootOutputPath).Empty(file => file.Name.StartsWith('.'));
        }

        using (new TimedLoggedOperation(Log, "Generating C# files")) {
            BindingsCodeWriter bindingsWriter = new(compilationUnits, BindingsContext);
            bindingsWriter.SaveToDirectory();
        }

        totalStopwatch.Stop();
        Log.LogInformation($"Done! (total time {totalStopwatch.Elapsed.TotalSeconds:0.00} s)");
    }

    private BindingsModel ResolveBindingsModel(MetadataModel metadataModel) {
        BindingsModelFromMetadataResolver bindingsModelResolver = new(
            metadataModel,
            BindingsContext
        );

        bindingsModelResolver.UnresolvedTypeDetected += entity => {
            switch (entity) {
                case MetadataModel.Builtin builtin:
                    if (KnownBindingsEntitiesInfo.ExpectedTypesDefinedElsewhere.Contains(builtin.Type))
                        return;

                    break;
            }

            throw new KeyNotFoundException(
                $"Unresolved entity type, make sure you've included all necessary metadata: {entity}"
            );
        };

        return bindingsModelResolver.Resolve();
    }

    private static BindingsFileCustomizations LoadBindingsFileCustomizations(string filePath) {
        Log.LogInformation($"Loading customizations file '{filePath}'");
        string json = File.ReadAllText(filePath);
        return BindingsFileCustomizationsDeserializer.Deserialize(json);
    }

    private static BindingsCustomizationsContext LoadBindingsCustomizationsContext() {
        string programPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string resourcesPath = Path.GetRelativePath(".", Path.Combine(programPath, "Resources", "Customizations"));
        if (!Directory.Exists(resourcesPath))
            throw new DirectoryNotFoundException($"Bindings customizations directory doesn't exist: '{resourcesPath}'");

        string[] jsonFiles = Directory.GetFiles(resourcesPath, "*.json", SearchOption.AllDirectories);
        Log.LogInformation($"Loading bindings customizations files from '{resourcesPath}' ({jsonFiles.Length} files found)");

        BindingsFileCustomizations[] fileCustomizations =
            jsonFiles
            .Select(LoadBindingsFileCustomizations)
            .ToArray();

        return new BindingsCustomizationsContext(fileCustomizations);
    }

    private static MetadataDto LoadDearBindingsDto(string filePath) {
        Log.LogInformation($"Loading metadata file '{filePath}'");
        string json = File.ReadAllText(filePath);
        return MetadataDtoDeserializer.Deserialize(json);
    }

    // Entities from the latter DTOs overwrite entities from earlier ones
    // based on entity name and preprocessor conditionals
    private static MetadataDto MergeDearBindingsDtos(IEnumerable<MetadataDto> dtos) {
        return new MetadataDto(
            Merge(dto => dto.Enums, x => x.Name),
            Merge(dto => dto.Defines, x => x.Name),
            Merge(dto => dto.Typedefs, x => x.Name),
            Merge(dto => dto.Structs, x => x.Name),
            Merge(dto => dto.Functions, x => x.Name)
        );

        List<T> Merge<T>(Func<MetadataDto, IEnumerable<T>> enumerableGetter, Func<T, string> nameGetter)
            where T : MetadataDto.BaseEntity {
            HashSet<T> set =
                new(EqualityComparer<T>.Create(
                    (x, y) => nameGetter(x!).Equals(nameGetter(y!)) && x!.Conditionals.SequenceEqual(y!.Conditionals),
                    x => nameGetter(x).GetHashCode())
                );

            foreach (MetadataDto dto in dtos) {
                T[] dtoItems = enumerableGetter(dto).ToArray();
                set.ExceptWith(dtoItems);
                set.UnionWith(dtoItems);
            }

            return set.ToList();
        }
    }
}
