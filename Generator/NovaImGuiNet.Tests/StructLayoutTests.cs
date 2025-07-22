using System.Reflection;
using System.Runtime.InteropServices;
using NovaImGuiNet.Internal;
using NovaImGuiNet.Interop;

namespace NovaImGuiNet.Tests;

public unsafe class StructLayoutTests {
    [Test]
    public void SizeOf() {
        Assert.Multiple(() =>
        {
            Assert.That(sizeof(bool), Is.EqualTo(1));
            Assert.That(sizeof(Pointer<int>), Is.EqualTo(sizeof(IntPtr)));

            Assert.That(Marshal.SizeOf<ImGuiTableColumnSortSpecs>(), Is.EqualTo(12));
            Assert.That(sizeof(ImGuiTableColumnSortSpecs), Is.EqualTo(12));

            Assert.That(Marshal.SizeOf<ImGuiStackLevelInfo>(), Is.EqualTo(72));
            Assert.That(sizeof(ImGuiStackLevelInfo), Is.EqualTo(72));

            Assert.That(Marshal.SizeOf<ImGuiBoxSelectState>(), Is.EqualTo(104));
            Assert.That(sizeof(ImGuiBoxSelectState), Is.EqualTo(104));

            Assert.That(Marshal.SizeOf<ImGuiIO>(), Is.EqualTo(14616));
            Assert.That(sizeof(ImGuiIO), Is.EqualTo(14616));

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            Assert.That(sizeof(ImGuiUtf8StringWriter), Is.GreaterThanOrEqualTo(Utf8StringMarshaller.StackAllocMaxSize));

            //Assert.That(Marshal.SizeOf<ImGuiDockNode>(), Is.EqualTo(208));
            //Assert.That(sizeof(ImGuiDockNode), Is.EqualTo(208));
        });
    }

    [Test]
    public void ImGUIIO_Layout() {
        Type type = typeof(ImGuiIO);
        Console.WriteLine($"sizeof({nameof(ImGuiIO)}): {sizeof(ImGuiIO)}");
        FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int index = 0; index < fieldInfos.Length; index++) {
            FieldInfo fieldInfo = fieldInfos[index];
            IntPtr fieldOffset = Marshal.OffsetOf(type, fieldInfo.Name);
            Console.WriteLine($"[{index + 1}] {fieldInfo.Name}, offset {fieldOffset} (0x{fieldOffset.ToInt32():x4})");
        }
    }

    [Test]
    public void Equals() {
        ImGuiKeyData foo1 = default;
        ImGuiKeyData foo2 = new();

        ImGuiKeyData foo3 = new() {
            DownDuration = 1.0f
        };

        Assert.That(foo1, Is.EqualTo(foo2));
        Assert.That(foo2, Is.Not.EqualTo(foo3));
    }

    public unsafe partial struct ImGuiStackLevelInfo {
        public uint ID;
        public byte QueryFrameCount;
        [MarshalAs(UnmanagedType.I1)] public bool QuerySuccess;
        public int DataType;
        public fixed byte Desc[57];
    }
}
