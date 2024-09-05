using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public class Generator {
    private static readonly ILogger Log = Logging.Create<Generator>();

    private BindingsContext BindingsContext { get; }

    public Generator(BindingsContext bindingsContext) {
        BindingsContext = bindingsContext;
    }

    private MetadataDto LoadDearBindingsDto(string filePath) {
        Log.LogInformation($"Loading {filePath}");
        string json = File.ReadAllText(filePath);
        return MetadataDtoDeserializer.Deserialize(json);
    }

    // Entities from the latter DTOs overwrite entities from earlier ones
    // based on entity name and preprocessor conditionals
    private MetadataDto MergeDearBindingsDtos(IEnumerable<MetadataDto> dtos) {
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

    public void Execute() {
        Stopwatch totalStopwatch = Stopwatch.StartNew();

        MetadataDto metadataDto;
        using (new TimedLoggedOperation(Log, "Parsing metadata")) {
            metadataDto = MergeDearBindingsDtos([
                //LoadDearBindingsDto("Resources/cimgui/cimgui_impl_sdl2.json"),
                //LoadDearBindingsDto("Resources/cimgui/cimgui_impl_sdlrenderer2.json"),
                //LoadDearBindingsDto("Resources/cimgui/cimgui_impl_win32.json"),
                LoadDearBindingsDto("Resources/cimgui/cimgui_imconfig.json"),
                LoadDearBindingsDto("Resources/cimgui/cimgui.json"),
                LoadDearBindingsDto("Resources/cimgui/cimgui_internal.json"),
                LoadDearBindingsDto("Resources/cimgui/cimgui_internal_imstb_textedit_minimal.json"),
            ]);

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

        using (new TimedLoggedOperation(Log, "Applying transformations")) {
            bindingsModel =
                new BindingsModelCSharpTransformer(
                    bindingsModel,
                    metadataDto,
                    BindingsContext
                ).Transform();
        }

        IReadOnlyList<BindingsCompilationUnit> compilationUnits;
        using (new TimedLoggedOperation(Log, "Generating C# code")) {
            BindingsModelCodeGenerator codeGenerator = new(bindingsModel, BindingsContext);
            compilationUnits = codeGenerator.Generate();
        }

        if (!Directory.Exists(BindingsContext.RootOutputPath))
            throw new DirectoryNotFoundException($"Directory '{BindingsContext.RootOutputPath}' doesn't exist");

        using (new TimedLoggedOperation(Log, "Cleaning output directories")) {
            new DirectoryInfo(BindingsContext.RootOutputPath).Empty(file => file.Name.StartsWith('.'));
        }

        using (new TimedLoggedOperation(Log, "Writing files")) {
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
}
