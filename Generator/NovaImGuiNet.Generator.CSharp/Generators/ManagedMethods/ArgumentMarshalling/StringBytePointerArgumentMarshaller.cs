using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringBytePointerArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        if (Argument.IsPartOfDeclaration) {
            return new ArgumentMarshallerOutput(Argument.MarshalledArgument.Name, []);
        }

        return new ArgumentMarshallerOutput(
            InvocationVariable,
            CreateDefaultValueMarshallStatements()
        );
    }

    private List<ManagedMethodArgumentMarshallerStatement> CreateDefaultValueMarshallStatements() {
        string value;
        Debug.Assert(Argument.MarshalledArgument.DefaultValue != null);
        string defaultValue = Argument.MarshalledArgument.DefaultValue;
        if (!KnownEntities.NullishValues.Contains(defaultValue)) {
            return [$"{NativeArgumentType} {InvocationVariable} = InteropUtility.SpanToPointer({defaultValue}u8);"];
        } else {
            value = defaultValue;
        }

        // Special optimization for when the end value in begin/end pair is default-only.
        // Instead of just passing the default value, we calculate the end pointer from what we know.
        if (Argument.MarshallingVariant.StringBeginEndPairMode == StringBeginEndPairMode.EndDefault) {
            ManagedMethodArgument pairBeginArgument = MethodSignature.Arguments[Argument.ArgumentIndex - 1];
            Debug.Assert(pairBeginArgument.MarshallingVariant.StringBeginEndPairMode == StringBeginEndPairMode.BeginOnly);
            Debug.Assert(KnownEntities.NullishValues.Contains(Argument.MarshalledArgument.DefaultValue));

            switch (pairBeginArgument.MarshallingVariant.ArgumentMarshallingMode) {
                case ArgumentMarshallingMode.StringBytePointer:
                    break;
                case ArgumentMarshallingMode.StringReadOnlyByteSpan:
                    return [
                        $"{MarshalledArgumentType} {InvocationVariable} = " +
                        $"{pairBeginArgument.NativeArgument.Name}_in_native + {pairBeginArgument.MarshalledArgument.Name}.Length;"
                    ];
                case ArgumentMarshallingMode.StringReadOnlyCharSpan:
                case ArgumentMarshallingMode.StringManaged:
                    // See also: StringReadOnlyCharSpanArgumentMarshaller
                    return [
                        $"{MarshalledArgumentType} {InvocationVariable} = " +
                        $"{pairBeginArgument.NativeArgument.Name}_in_native + {pairBeginArgument.NativeArgument.Name}_native_utf8_buffer_bytes_written;"
                    ];
                default:
                    throw new ArgumentOutOfRangeException(
                        $"Unexpected pairBeginArgument.MarshallingVariant.ArgumentMarshallingMode: " +
                        $"{pairBeginArgument.MarshallingVariant.ArgumentMarshallingMode}"
                    );
            }
        }

        return [$"{MarshalledArgumentType} {InvocationVariable} = {value};"];
    }
}
