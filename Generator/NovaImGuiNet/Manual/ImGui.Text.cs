namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    ///<summary>
    ///  Widgets: Text
    ///  raw text without formatting. Roughly equivalent to Text(&quot;%s&quot;, text) but: A) doesn&#39;t require null terminated string if &#39;text_end&#39; is specified, B) it&#39;s faster, no memory copy is done, no buffer size limits, recommended for long chunks of text.
    ///</summary>
    public static void Text(ReadOnlySpan<byte> text) => TextUnformatted(text);

    ///<summary>
    ///  Widgets: Text
    ///  raw text without formatting. Roughly equivalent to Text(&quot;%s&quot;, text) but: A) doesn&#39;t require null terminated string if &#39;text_end&#39; is specified, B) it&#39;s faster, no memory copy is done, no buffer size limits, recommended for long chunks of text.
    ///</summary>
    public static void Text(ReadOnlySpan<char> text) => TextUnformatted(text);

#if NETSTANDARD2_0
    ///<summary>
    ///  Widgets: Text
    ///  raw text without formatting. Roughly equivalent to Text(&quot;%s&quot;, text) but: A) doesn&#39;t require null terminated string if &#39;text_end&#39; is specified, B) it&#39;s faster, no memory copy is done, no buffer size limits, recommended for long chunks of text.
    ///</summary>
    public static void Text(string text) => TextUnformatted(text);
#endif

    ///<summary>
    ///  Widgets: Text
    ///  raw text without formatting. Roughly equivalent to Text(&quot;%s&quot;, text) but: A) doesn&#39;t require null terminated string if &#39;text_end&#39; is specified, B) it&#39;s faster, no memory copy is done, no buffer size limits, recommended for long chunks of text.
    ///</summary>
    public static void Text(byte* text, byte* textEnd = default) => TextUnformatted(text, textEnd);
}
