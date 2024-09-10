using System.Runtime.InteropServices;
using NovaImGuiNet.Internal;

namespace NovaImGuiNet.Tests;

public unsafe class StructSizeTests {
    [Test]
    public void SizeOf() {
        Assert.That(sizeof(bool), Is.EqualTo(1));

        Assert.That(Marshal.SizeOf<ImGuiTableColumnSortSpecs>(), Is.EqualTo(12));
        Assert.That(sizeof(ImGuiTableColumnSortSpecs), Is.EqualTo(12));

        Assert.That(Marshal.SizeOf<ImGuiStackLevelInfo>(), Is.EqualTo(72));
        Assert.That(sizeof(ImGuiStackLevelInfo), Is.EqualTo(72));

        Assert.That(Marshal.SizeOf<ImGuiBoxSelectState>(), Is.EqualTo(104));
        Assert.That(sizeof(ImGuiBoxSelectState), Is.EqualTo(104));

        //Assert.That(Marshal.SizeOf<ImGuiDockNode>(), Is.EqualTo(208));
        //Assert.That(sizeof(ImGuiDockNode), Is.EqualTo(208));
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

    public unsafe partial struct ImGuiStackLevelInfo
    {
        public uint ID;
        public byte QueryFrameCount;
        [MarshalAs(UnmanagedType.I1)] public bool QuerySuccess;
        public int DataType;
        public fixed byte Desc[57];
    }
}
