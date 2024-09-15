using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator;

public readonly struct TimedLoggedOperation : IDisposable {
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly ILogger _logger;
    private readonly string _operationName;

    public TimedLoggedOperation(ILogger logger, string operationName, bool noStartLog = false) {
        _logger = logger;
        _operationName = operationName;

        if (!noStartLog) {
            _logger.LogInformation($"{_operationName}");
        }
    }

    public void Dispose() {
        _stopwatch.Stop();
        _logger.LogTrace($"'{_operationName}' took {_stopwatch.ElapsedMilliseconds} ms");
    }
}
