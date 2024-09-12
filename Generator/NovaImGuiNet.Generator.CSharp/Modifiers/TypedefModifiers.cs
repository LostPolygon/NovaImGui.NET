using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;

public static class TypedefModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(TypedefModifiers));

    public static Typedef UpdateEnumTypedefs(Typedef typedef, BindingsModel model) {
        // In Dear ImGui code, enums are defined as a C++ enum,
        // plus a typedef specifying the underlying type.
        //
        // 1) Match up the enum and its typedef
        // 2) Update the underlying enum type from typedef
        // 3) Point the typedef to the target enum
        EntityReferenceWrapper<Enum>? matchingEnum =
            model.Enums
                .FirstOrDefault(x =>
                    x.Value.Name == typedef.Name ||
                    x.Value.Name == typedef.Name + "_"
                );
        if (matchingEnum == null) {
            bool isUnexpectedMismatch = typedef.Comments.Attached.Any(c => c.StartsWith("-> enum"));
            if (isUnexpectedMismatch) {
                Log.LogWarning(
                    $"Typedef '{FormatType(typedef)}' " +
                    $"was expected to have a matching enum , but none was found"
                );
            }

            return typedef;
        }

        matchingEnum.Value.UnderlyingType = typedef.Type;
        typedef.Type = matchingEnum.Reference;
        typedef.NoEmit = true;

        return typedef;
    }
}
