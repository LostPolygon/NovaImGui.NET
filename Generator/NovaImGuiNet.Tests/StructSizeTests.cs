using System.Runtime.InteropServices;

namespace NovaImGuiNet.Tests;

public unsafe class StructSizeTests {
    [Test]
    public void SizeOf() {
        Assert.That(Marshal.SizeOf<ImGuiStackLevelInfo>(), Is.EqualTo(72));
        Assert.That(sizeof(ImGuiStackLevelInfo), Is.EqualTo(72));

        Assert.That(Marshal.SizeOf<ImGuiBoxSelectState>(), Is.EqualTo(104));
        Assert.That(sizeof(ImGuiBoxSelectState), Is.EqualTo(104));

        //Assert.That(Marshal.SizeOf<ImGuiDockNode>(), Is.EqualTo(208));
        //Assert.That(sizeof(ImGuiDockNode), Is.EqualTo(208));
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
