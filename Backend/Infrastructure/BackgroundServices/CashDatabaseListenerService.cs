using KtcWeb.Hubs;
using KtcWeb.Models.Monitoring;
using Microsoft.AspNetCore.SignalR;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;
using ErrorEventArgsT = TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs;

namespace KtcWeb.Infrastructure.BackgroundServices;

/// <summary>
/// Écoute les modifications sur les tables de cash cassettes (PhysicalCassette, CurrentCashUnitStatus)
/// et propage les mises à jour en temps réel via SignalR.
/// </summary>
public sealed class CashDatabaseListenerService : IHostedService, IDisposable
{
    private readonly IHubContext<KtcMonitoringHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CashDatabaseListenerService> _logger;

    private SqlTableDependency<PhysicalCassetteDependency>? _cassetteDependency;
    private SqlTableDependency<CurrentCashUnitStatusDependency>? _cashStatusDependency;

    public CashDatabaseListenerService(
        IHubContext<KtcMonitoringHub> hubContext,
        IConfiguration configuration,
        ILogger<CashDatabaseListenerService> logger)
    {
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("KtcDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("KtcDb connection string missing; CashDatabaseListenerService not started.");
            return Task.CompletedTask;
        }

        try
        {
            // Listen on PhysicalCassette table
            _cassetteDependency = new SqlTableDependency<PhysicalCassetteDependency>(
                connectionString,
                tableName: "PhysicalCassette",
                mapper: null,
                notifyOn: DmlTriggerType.All,
                executeUserPermissionCheck: true,
                includeOldValues: false);

            _cassetteDependency.OnChanged += (sender, e) => OnCassetteChanged(e);
            _cassetteDependency.OnError += OnError;
            _cassetteDependency.Start();
            _logger.LogInformation("SqlTableDependency started on PhysicalCassette table.");

            // Listen on CurrentCashUnitStatus table
            _cashStatusDependency = new SqlTableDependency<CurrentCashUnitStatusDependency>(
                connectionString,
                tableName: "CurrentCashUnitStatus",
                mapper: null,
                notifyOn: DmlTriggerType.All,
                executeUserPermissionCheck: true,
                includeOldValues: false);

            _cashStatusDependency.OnChanged += (sender, e) => OnCashStatusChanged(e);
            _cashStatusDependency.OnError += OnError;
            _cashStatusDependency.Start();
            _logger.LogInformation("SqlTableDependency started on CurrentCashUnitStatus table.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start CashDatabaseListenerService; real-time cash updates will not be pushed.");
        }

        return Task.CompletedTask;
    }

    private void OnCassetteChanged(RecordChangedEventArgs<PhysicalCassetteDependency> e)
    {
        if (e.Entity == null)
            return;

        if (e.ChangeType != ChangeType.Insert && e.ChangeType != ChangeType.Update)
            return;

        _logger.LogInformation("PhysicalCassette notified: {ChangeType}, ClientId={ClientId}, CassetteId={CassetteId}",
            e.ChangeType, e.Entity.ClientId, e.Entity.CassetteId);

        _ = BroadcastCassetteAsync(e.Entity);
    }

    private void OnCashStatusChanged(RecordChangedEventArgs<CurrentCashUnitStatusDependency> e)
    {
        if (e.Entity == null)
            return;

        if (e.ChangeType != ChangeType.Insert && e.ChangeType != ChangeType.Update)
            return;

        _logger.LogInformation("CurrentCashUnitStatus notified: {ChangeType}, ClientId={ClientId}, CashUnitId={CashUnitId}",
            e.ChangeType, e.Entity.ClientId, e.Entity.CashUnitId);

        _ = BroadcastCashStatusAsync(e.Entity);
    }

    private async Task BroadcastCassetteAsync(PhysicalCassetteDependency entity)
    {
        try
        {
            var groupName = $"client_{entity.ClientId}_cassettes";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveCassetteUpdate", entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR push failed for PhysicalCassette update.");
        }
    }

    private async Task BroadcastCashStatusAsync(CurrentCashUnitStatusDependency entity)
    {
        try
        {
            var groupName = $"client_{entity.ClientId}_cashstatus";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveCashStatusUpdate", entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR push failed for CurrentCashUnitStatus update.");
        }
    }

    private void OnError(object? sender, ErrorEventArgsT e)
    {
        _logger.LogError(e.Error, "SqlTableDependency error in CashDatabaseListenerService");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_cassetteDependency != null)
            {
                _cassetteDependency.OnChanged -= (sender, e) => OnCassetteChanged(e);
                _cassetteDependency.OnError -= OnError;
                _cassetteDependency.Stop();
                _cassetteDependency.Dispose();
                _cassetteDependency = null;
                _logger.LogInformation("PhysicalCassette listener stopped.");
            }

            if (_cashStatusDependency != null)
            {
                _cashStatusDependency.OnChanged -= (sender, e) => OnCashStatusChanged(e);
                _cashStatusDependency.OnError -= OnError;
                _cashStatusDependency.Stop();
                _cashStatusDependency.Dispose();
                _cashStatusDependency = null;
                _logger.LogInformation("CurrentCashUnitStatus listener stopped.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping CashDatabaseListenerService.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _cassetteDependency?.Stop();
            _cassetteDependency?.Dispose();
            _cashStatusDependency?.Stop();
            _cashStatusDependency?.Dispose();
        }
        catch
        {
            // ignored
        }

        _cassetteDependency = null;
        _cashStatusDependency = null;
    }
}
