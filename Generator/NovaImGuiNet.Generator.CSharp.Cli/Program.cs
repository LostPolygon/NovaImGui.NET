using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator;
using NovaImGuiNet.Generator.CSharp;

Logging.SetLoggerFactory(
    LoggerFactory.Create(builder =>
        builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddSimpleConsole(options => {
                options.IncludeScopes = true;
                options.SingleLine = true;
                //options.TimestampFormat = "HH:mm:ss ";
            })
        )
);

if (args.Length < 1 || String.IsNullOrWhiteSpace(args[0]))
    throw new ArgumentException("Empty path");

string outputPath = args[0];

CodeFormatMode codeFormatMode = CodeFormatMode.Fancy;
#if true || DEBUG
    codeFormatMode = CodeFormatMode.Basic;
#endif

BindingsContext bindingsContext = new(
    "NovaImGuiNet",
    outputPath,
    new BindingsGenerationOptions() {
        CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes = true,
        CodeFormatMode = codeFormatMode
    }
);

Generator generator = new(bindingsContext);
generator.Execute();
