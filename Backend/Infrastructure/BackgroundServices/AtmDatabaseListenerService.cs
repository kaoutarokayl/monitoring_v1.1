using KtcWeb.Application.DTOs;
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
/// Écoute les INSERT sur dbo.Clients via Service Broker (SqlTableDependency) et propage via SignalR.
/// </summary>
public sealed class AtmDatabaseListenerService : IHostedService, IDisposable
{
    private readonly IHubContext<KtcMonitoringHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AtmDatabaseListenerService> _logger;
    private SqlTableDependency<ClientAtmDependency>? _dependency;

    public AtmDatabaseListenerService(
        IHubContext<KtcMonitoringHub> hubContext,
        IConfiguration configuration,
        ILogger<AtmDatabaseListenerService> logger)
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
            _logger.LogWarning("KtcDb connection string missing; AtmDatabaseListenerService not started.");
            return Task.CompletedTask;
        }

        try
        {
            // Use ClientAtmDependency model which excludes the XML Comments column
            _dependency = new SqlTableDependency<ClientAtmDependency>(
                connectionString,
                tableName: "Clients",
                schemaName: "dbo",
                mapper: null, // Automatic mapping since model excludes XML column
                updateOf: null,
                filter: null,
                // INSERT seul peut manquer certains flux (outil qui INSERT puis UPDATE, BULK…).
                notifyOn: DmlTriggerType.All,
                executeUserPermissionCheck: true,
                includeOldValues: false);

            _dependency.OnChanged += OnTableChanged;
            _dependency.OnError += OnError;
            _dependency.Start();
            _logger.LogInformation("SqlTableDependency started on dbo.Clients (Insert/Update/Delete notifications).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SqlTableDependency; real-time ATM inserts will not be pushed.");
        }

        return Task.CompletedTask;
    }

    private void OnError(object? sender, ErrorEventArgsT e)
    {
        _logger.LogError(e.Error, "SqlTableDependency error");
    }

    private void OnTableChanged(object? sender, RecordChangedEventArgs<ClientAtmDependency> e)
    {
        if (e.Entity == null)
            return;

        if (e.ChangeType != ChangeType.Insert && e.ChangeType != ChangeType.Update)
            return;

        _logger.LogInformation("dbo.Clients notified: {ChangeType}, ClientId={ClientId}",
            e.ChangeType, e.Entity.ClientId);

        _ = PushClientRowAsync(e.Entity);
    }

    private async Task PushClientRowAsync(ClientAtmDependency row)
    {
        try
        {
            var dto = MapToDto(row);
            await _hubContext.Clients.All.SendAsync("ReceiveNewData", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR push failed after Clients change.");
        }
    }

    private static ClientAtmDto MapToDto(ClientAtmDependency row) => new()
    {
        ClientId = row.ClientId,
        KtcGuid = row.KtcGuid.ToString(),
        ClientName = row.ClientName,
        NetworkAddress = row.NetworkAddress,
        Connectable = row.Connectable,
        DetailsUnknown = row.DetailsUnknown,
        Latitude = row.Latitude,
        Longitude = row.Longitude,
        Timezone = row.Timezone,
        Comments = null, // Not available in dependency model
        BusinessId = row.BusinessId,
        BranchId = row.BranchId,
        HardwareTypeId = row.HardwareTypeId,
        HardwareTypeName = null,
        Active = row.Active,
        ClientType = row.ClientType
    };

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_dependency != null)
            {
                _dependency.OnChanged -= OnTableChanged;
                _dependency.OnError -= OnError;
                _dependency.Stop();
                _dependency.Dispose();
                _dependency = null;
                _logger.LogInformation("SqlTableDependency stopped.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping SqlTableDependency.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _dependency?.Stop();
            _dependency?.Dispose();
        }
        catch
        {
            // ignored
        }

        _dependency = null;
    }
}
