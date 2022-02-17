using Renci.SshNet;
using Session = Common.Model.Session;

namespace PollingLoginSshWorkerService;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger<WindowsBackgroundService> _logger;
    private readonly SessionService _sessionService;
    private Session? _session;
    private SshClient? _sshClient;

    public WindowsBackgroundService(
        SessionService sessionService,
        ILogger<WindowsBackgroundService> logger)
    {
        (_sessionService, _logger) = (sessionService, logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var newSession = await _sessionService.GetSessionAsync();

            _logger.LogInformation("newSession:" + newSession);
            _logger.LogInformation("_session:" + _session);

            if (newSession != null)
            {
                if (!newSession.Equals(_session))
                {
                    _session = newSession;
                    CloseConnection();
                    var connectionInfo = new ConnectionInfo(_session.IpAddress, _session.Port,
                        _session.Username, new PasswordAuthenticationMethod(_session.Username, _session.Password));
                    _sshClient = new SshClient(connectionInfo);
                    _sshClient.Connect();
                    _sshClient.AddForwardedPort(new ForwardedPortRemote(3389, "localhost", 3389));
                    foreach (var clientForwardedPort in _sshClient.ForwardedPorts)
                    {
                        clientForwardedPort.Start();
                        _logger.LogInformation("ForwardedPortRemote: " + clientForwardedPort.IsStarted);
                    }
                }
            }
            else
            {
                CloseConnection();
                _session = null;
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private void CloseConnection()
    {
        if (_sshClient!.IsConnected) _sshClient.Disconnect();
        _sshClient!.Dispose();
        _sshClient = null;
        _logger.LogInformation("Close SSH client.");
    }
}