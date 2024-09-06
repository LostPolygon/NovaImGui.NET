using System.Text;
using NovaImGuiNet.Interop;

namespace NovaImGuiNet.Tests;

public unsafe class InteropTests {
    [Test]
    public void CharSpanToPointer() {
        ReadOnlySpan<char> emptyStringSpan = "".AsSpan();
        ReadOnlySpan<char> emptyCharSpan = default;

        char* emptyStringSpanPtr = InteropUtility.SpanToPointer(emptyStringSpan);
        char* emptyCharSpanPtr = InteropUtility.SpanToPointer(emptyCharSpan);

        Assert.That((IntPtr) emptyStringSpanPtr, Is.Not.EqualTo(IntPtr.Zero));
        Assert.That((IntPtr) emptyCharSpanPtr, Is.EqualTo(IntPtr.Zero));
    }

    [Test]
    public void Utf8StringMarshaller_EstimateNullTerminatedUtf8StringByteCount() {
        Assert.That(Utf8StringMarshaller.EstimateNullTerminatedUtf8StringByteCount(0), Is.AtLeast(0));
        Assert.That(Utf8StringMarshaller.EstimateNullTerminatedUtf8StringByteCount(1), Is.AtLeast(7));
        Assert.That(Utf8StringMarshaller.EstimateNullTerminatedUtf8StringByteCount(300), Is.AtLeast(904));
    }
}
