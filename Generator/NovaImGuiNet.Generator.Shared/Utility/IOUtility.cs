namespace NovaImGuiNet.Generator;

public static class IOUtility {
    public static void Empty(this DirectoryInfo directory, Func<FileInfo, bool>? skipDeleteFileMatcher = null) {
        foreach (FileInfo file in directory.GetFiles()) {
            if (skipDeleteFileMatcher != null && skipDeleteFileMatcher(file))
                continue;

            file.Delete();
        }

        foreach (DirectoryInfo subDirectory in directory.GetDirectories()) {
            subDirectory.Delete(true);
        }
    }
}
