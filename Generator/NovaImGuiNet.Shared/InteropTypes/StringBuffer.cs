using System.Text;

namespace NovaImGuiNet;

public struct StringBuffer {
    private byte[] _buffer;

    public StringBuffer() {
        _buffer = [];
    }

    public StringBuffer(int initialSizeHint) {
        _buffer = new byte[initialSizeHint];
    }

    public override string ToString() {
        return Encoding.UTF8.GetString(_buffer);
    }

    public void HandleResizeCallback() {

    }
}
