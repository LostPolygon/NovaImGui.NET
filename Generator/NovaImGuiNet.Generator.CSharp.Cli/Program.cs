using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator;
using NovaImGuiNet.Generator.CSharp;

ILoggerFactory loggerFactory =
    LoggerFactory.Create(builder =>
        builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddSimpleConsole(options => {
                options.IncludeScopes = true;
                options.SingleLine = true;

                //options.TimestampFormat = "HH:mm:ss ";
            })
    );

Logging.SetLoggerFactory(loggerFactory);

ILogger log = Logging.Create(nameof(Program));

if (args.Length < 1 || String.IsNullOrWhiteSpace(args[0]))
    throw new ArgumentException("Empty path");

string outputPath = args[0];

CodeFormatMode codeFormatMode = CodeFormatMode.Fancy;
bool generateXmlDocs = true;
#if true || DEBUG
//codeFormatMode = CodeFormatMode.Basic;
//generateXmlDocs = false;
#endif

BindingsGenerationOptions bindingsGenerationOptions = new() {
    CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes = true,
    GenerateXmlDocumentationComments = generateXmlDocs,
    CodeFormatMode = codeFormatMode
};

#if !DEBUG
try {
#else
{
#endif
    Generator generator = new(outputPath, bindingsGenerationOptions);
    generator.Execute();
    loggerFactory.Dispose();
#if !DEBUG
} catch (Exception e) {
    log.LogCritical("Error while running generator");
    loggerFactory.Dispose();
    await Task.Delay(TimeSpan.FromSeconds(0.1));

    Console.WriteLine(e.ToString());
    Environment.Exit(1);
}
#else
}
#endif
