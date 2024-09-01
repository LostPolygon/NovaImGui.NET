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

    public static BindingsModel RewriteTypedefsToEnums(BindingsModel model) {
        // If there are typedefs that don't point to an enum,
        // but have the same(ish) name as an enum, changed them to point to an actual enum
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

            typedef.Value.Type = matchingEnum.Reference;
            typedef.Value.SkipEmit = true;
        }

        return model;
    }
}
