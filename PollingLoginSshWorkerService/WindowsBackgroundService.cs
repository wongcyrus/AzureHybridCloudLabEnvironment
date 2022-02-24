using Renci.SshNet;
using Session = Common.Model.Session;

namespace PollingLoginSshWorkerService;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly ILogger<WindowsBackgroundService> _logger;
    private readonly SessionService _sessionService;
    private string _lastErrorMessage = "";
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
            var newSession = _sessionService.GetSessionAsync(_sshClient != null, _lastErrorMessage);
            _lastErrorMessage = "";

            _logger.LogInformation("newSession:" + newSession);
            _logger.LogInformation("_session:" + _session);

            if (newSession != null)
            {
                if (newSession.Equals(_session))
                {
                    if (_sshClient is null or {IsConnected: false})
                    {
                        //Same session and reconnect.
                        _logger.LogInformation("Same session and reconnect: " + _session);
                        CloseConnection();
                        Connect();
                    }
                }
                else
                {
                    //New session.
                    _logger.LogInformation("New connection: " + _session);
                    _session = newSession;
                    CloseConnection();
                    Connect();
                }
            }
            else
            {
                _logger.LogInformation("No new session and close connection.");
                CloseConnection();
                _session = null;
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private void Connect()
    {
        if (_session == null)
        {
            _logger.LogInformation("Cannot connect with null session!");
            return;
        }

        try
        {
            var connectionInfo = new ConnectionInfo(_session.IpAddress, _session.Port,
                _session.Username, new PasswordAuthenticationMethod(_session.Username, _session.Password));
            _sshClient = new SshClient(connectionInfo);
            _sshClient.ErrorOccurred += (s, args) => _logger.LogInformation("sshClient:" + args.Exception.Message);
            _sshClient.Connect();
        }
        catch (Exception ex)
        {
            CloseConnection();
            _logger.LogError("Cannot connect to " + _session);
            _logger.LogError(ex.Message);
            _lastErrorMessage = $"Connect Error {DateTime.Now.ToUniversalTime().ToString()}: {ex.Message}";
            return;
        }

        uint[] portNumbers = {3389, 5900};
        foreach (var portNumber in portNumbers)
        {
            var port = new ForwardedPortRemote(portNumber, "localhost", portNumber);
            port.Exception += (s, args) => _logger.LogInformation("Port(" + portNumber + ")" + args.Exception.Message);
            _sshClient.AddForwardedPort(port);
            port.Start();
            _logger.LogInformation("ForwardedPortRemote IsStarted=" + port.IsStarted);
            port.RequestReceived += (sender, args) =>
                _logger.LogInformation("ForwardedPortRemote " + args.OriginatorHost + ":" + args.OriginatorPort);
        }
    }

    private void CloseConnection()
    {
        if (_sshClient == null) return;
        try
        {
            _sshClient.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            _lastErrorMessage = $"CloseConnection Error {DateTime.Now.ToUniversalTime().ToString()}: {ex.Message}";
        }
        finally
        {
            _sshClient = null;
            _logger.LogInformation("Close SSH client.");
        }
    }

    public override void Dispose()
    {
        CloseConnection();
        base.Dispose();
    }
}