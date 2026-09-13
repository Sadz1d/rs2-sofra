using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Sofra.Worker.Options;

namespace Sofra.Worker.Messaging;

/// <summary>
/// Drži jednu (singleton) RabbitMQ konekciju za život procesa Workera.
/// Konzumira je kasniji event consumer umjesto da svaki put otvara novu konekciju.
/// </summary>
public sealed class RabbitMqConnectionService(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnectionService> logger) : IHostedService, IAsyncDisposable
{
    private static readonly int[] BackoffSeconds = [1, 2, 4, 8];

    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;

    public IConnection Connection =>
        _connection ?? throw new InvalidOperationException("RabbitMQ konekcija nije uspostavljena.");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
        };

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                logger.LogInformation("Uspostavljena RabbitMQ konekcija na {Host}:{Port}.", _options.Host, _options.Port);
                return;
            }
            catch (Exception ex) when (attempt < BackoffSeconds.Length)
            {
                var delaySeconds = BackoffSeconds[attempt];
                logger.LogWarning(ex,
                    "Neuspjelo povezivanje na RabbitMQ ({Host}:{Port}), pokušaj {Attempt}/{MaxAttempts}, novi pokušaj za {Delay}s.",
                    _options.Host, _options.Port, attempt + 1, BackoffSeconds.Length, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _connection?.CloseAsync(cancellationToken) ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
