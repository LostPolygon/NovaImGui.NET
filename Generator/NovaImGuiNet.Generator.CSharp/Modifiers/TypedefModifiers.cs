namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class TypedefModifiers {
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

    public static Typedef? RemoveTypedefsToEnums(Typedef typedef) {
        if (typedef.Comments.Attached.Any(c => c.StartsWith("-> enum")))
            return null;

        return typedef;
    }
}
