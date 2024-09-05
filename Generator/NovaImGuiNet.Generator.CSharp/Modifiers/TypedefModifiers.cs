using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;

public static class TypedefModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(TypedefModifiers));

    public static Typedef PatchBrokenTypedefs(Typedef typedef) {
        switch (typedef.Name) {
            case "ImTextureID":
                typedef = typedef with {
                    Type = new Builtin("IntPtr", typeof(IntPtr), StorageClass.NotSpecified).AsEntityReference()
                };
                break;
        }

        return typedef;
    }

    public static BindingsModel UpdateEnumTypedefs(BindingsModel model) {
        // In Dear ImGui code, enums are defined as a C++ enum,
        // plus a typedef specifying the underlying type.
        //
        // 1) Match up the enum and its typedef
        // 2) Update the underlying enum type from typedef
        // 3) Point the typedef to the target enum
        foreach (EntityReferenceWrapper<Typedef> typedef in model.Typedefs) {
            EntityReferenceWrapper<Enum>? matchingEnum =
                model.Enums
                    .FirstOrDefault(x =>
                        x.Value.Name == typedef.Value.Name ||
                        x.Value.Name == typedef.Value.Name + "_"
                    );
            if (matchingEnum == null) {
                bool isUnexpectedMismatch = typedef.Value.Comments.Attached.Any(c => c.StartsWith("-> enum"));
                if (isUnexpectedMismatch) {
                    Log.LogWarning(
                        $"Typedef '{FormatType(typedef.Value)}' " +
                        $"was expected to have a matching enum , but none was found"
                    );
                }

                continue;
            }

            matchingEnum.Value.UnderlyingType = typedef.Value.Type;
            typedef.Value.Type = matchingEnum.Reference;
            typedef.Value.NoEmit = true;
        }

        return model;
    }
}
