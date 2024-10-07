namespace NovaImGuiNet;

public unsafe partial struct ImGuiTableSortSpecs {
    public Span<ImGuiTableColumnSortSpecs> SpecsSpan => new(Specs_, SpecsCount);
}
