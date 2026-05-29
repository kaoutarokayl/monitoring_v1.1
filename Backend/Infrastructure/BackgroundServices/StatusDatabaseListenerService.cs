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
/// Écoute les modifications sur CurrentStatus et AssetHistory via Service Broker (SqlTableDependency)
/// et propage les mises à jour en temps réel via SignalR.
/// </summary>
public sealed class StatusDatabaseListenerService : IHostedService, IDisposable
{
    private readonly IHubContext<KtcMonitoringHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StatusDatabaseListenerService> _logger;

    private SqlTableDependency<CurrentStatusDependency>? _currentStatusDependency;
    private SqlTableDependency<AssetHistoryDependency>? _assetHistoryDependency;

    public StatusDatabaseListenerService(
        IHubContext<KtcMonitoringHub> hubContext,
        IConfiguration configuration,
        ILogger<StatusDatabaseListenerService> logger)
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
            _logger.LogWarning("KtcDb connection string missing; StatusDatabaseListenerService not started.");
            return Task.CompletedTask;
        }

        // Separate try-catch so one failure doesn't block the other listener.
        try
        {
            // CurrentStatus has an xml 'data' column that SqlTableDependency doesn't support.
            // Explicit ModelToTableMapper + UpdateOfModel restricts validation to only the mapped columns.
            var statusMapper = new ModelToTableMapper<CurrentStatusDependency>();
            statusMapper.AddMapping(m => m.ClientId, "client_id");
            statusMapper.AddMapping(m => m.ComponentId, "component_id");
            statusMapper.AddMapping(m => m.PropertyId, "property_id");
            statusMapper.AddMapping(m => m.ValueId, "value_id");
            statusMapper.AddMapping(m => m.NumericValue, "numeric_value");

            var statusUpdateOf = new UpdateOfModel<CurrentStatusDependency>();
            statusUpdateOf.Add(m => m.ClientId);
            statusUpdateOf.Add(m => m.ComponentId);
            statusUpdateOf.Add(m => m.PropertyId);
            statusUpdateOf.Add(m => m.ValueId);
            statusUpdateOf.Add(m => m.NumericValue);

            _currentStatusDependency = new SqlTableDependency<CurrentStatusDependency>(
                connectionString,
                tableName: "CurrentStatus",
                schemaName: null,
                mapper: statusMapper,
                updateOf: statusUpdateOf,
                filter: null,
                notifyOn: DmlTriggerType.All,
                executeUserPermissionCheck: true,
                includeOldValues: false);

            _currentStatusDependency.OnChanged += (sender, e) => OnCurrentStatusChanged(e);
            _currentStatusDependency.OnError += OnError;
            _currentStatusDependency.Start();
            _logger.LogInformation("SqlTableDependency started on CurrentStatus table.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start CurrentStatus listener; status real-time updates will not be pushed.");
        }

        try
        {
            // Explicit mapper required: [Column] EF attributes are not recognized by SqlTableDependency with mapper: null.
            var assetMapper = new ModelToTableMapper<AssetHistoryDependency>();
            assetMapper.AddMapping(m => m.ClientId, "client_id");
            assetMapper.AddMapping(m => m.Date, "timestmp");
            assetMapper.AddMapping(m => m.ComponentId, "component_id");
            assetMapper.AddMapping(m => m.PropertyId, "property_id");
            assetMapper.AddMapping(m => m.OldValueId, "old_value_id");
            assetMapper.AddMapping(m => m.NewValueId, "new_value_id");
            assetMapper.AddMapping(m => m.OldNumericValue, "old_numeric_value");
            assetMapper.AddMapping(m => m.NewNumericValue, "new_numeric_value");
            assetMapper.AddMapping(m => m.Comments, "comments");

            var assetUpdateOf = new UpdateOfModel<AssetHistoryDependency>();
            assetUpdateOf.Add(m => m.ClientId);
            assetUpdateOf.Add(m => m.Date);
            assetUpdateOf.Add(m => m.ComponentId);
            assetUpdateOf.Add(m => m.PropertyId);
            assetUpdateOf.Add(m => m.OldValueId);
            assetUpdateOf.Add(m => m.NewValueId);
            assetUpdateOf.Add(m => m.OldNumericValue);
            assetUpdateOf.Add(m => m.NewNumericValue);
            assetUpdateOf.Add(m => m.Comments);

            _assetHistoryDependency = new SqlTableDependency<AssetHistoryDependency>(
                connectionString,
                tableName: "AssetHistory",
                schemaName: null,
                mapper: assetMapper,
                updateOf: assetUpdateOf,
                filter: null,
                notifyOn: DmlTriggerType.All,
                executeUserPermissionCheck: true,
                includeOldValues: false);

            _assetHistoryDependency.OnChanged += (sender, e) => OnAssetHistoryChanged(e);
            _assetHistoryDependency.OnError += OnError;
            _assetHistoryDependency.Start();
            _logger.LogInformation("SqlTableDependency started on AssetHistory table.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start AssetHistory listener; asset history real-time updates will not be pushed.");
        }

        return Task.CompletedTask;
    }

    private void OnCurrentStatusChanged(RecordChangedEventArgs<CurrentStatusDependency> e)
    {
        if (e.Entity == null)
            return;

        if (e.ChangeType != ChangeType.Insert && e.ChangeType != ChangeType.Update)
            return;

        _logger.LogInformation("CurrentStatus notified: {ChangeType}, ClientId={ClientId}, ComponentId={ComponentId}",
            e.ChangeType, e.Entity.ClientId, e.Entity.ComponentId);

        _ = BroadcastCurrentStatusAsync(e.Entity);
    }

    private void OnAssetHistoryChanged(RecordChangedEventArgs<AssetHistoryDependency> e)
    {
        if (e.Entity == null)
            return;

        if (e.ChangeType != ChangeType.Insert && e.ChangeType != ChangeType.Update)
            return;

        _logger.LogInformation("AssetHistory notified: {ChangeType}, ClientId={ClientId}",
            e.ChangeType, e.Entity.ClientId);

        _ = BroadcastAssetHistoryAsync(e.Entity);
    }

    private async Task BroadcastCurrentStatusAsync(CurrentStatusDependency entity)
    {
        try
        {
            var groupName = $"client_{entity.ClientId}_status";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveStatusUpdate", entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR push failed for CurrentStatus update.");
        }
    }

    private async Task BroadcastAssetHistoryAsync(AssetHistoryDependency entity)
    {
        try
        {
            var groupName = $"client_{entity.ClientId}_assets";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveAssetHistoryUpdate", entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR push failed for AssetHistory update.");
        }
    }

    private void OnError(object? sender, ErrorEventArgsT e)
    {
        _logger.LogError(e.Error, "SqlTableDependency error in StatusDatabaseListenerService");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _currentStatusDependency?.Stop();
            _currentStatusDependency?.Dispose();
            _currentStatusDependency = null;
            _logger.LogInformation("CurrentStatus listener stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping CurrentStatus listener.");
        }

        try
        {
            _assetHistoryDependency?.Stop();
            _assetHistoryDependency?.Dispose();
            _assetHistoryDependency = null;
            _logger.LogInformation("AssetHistory listener stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping AssetHistory listener.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _currentStatusDependency?.Stop();
            _currentStatusDependency?.Dispose();
            _assetHistoryDependency?.Stop();
            _assetHistoryDependency?.Dispose();
        }
        catch
        {
            // ignored
        }

        _currentStatusDependency = null;
        _assetHistoryDependency = null;
    }
}
