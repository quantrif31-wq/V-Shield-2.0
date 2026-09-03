using System.Globalization;
using System.Reflection;
using System.Text.Json;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace API.Services.Sync;

public class SyncEventApplier
{
    private readonly ApplicationDbContext _db;

    public SyncEventApplier(ApplicationDbContext db)
    {
        _db = db;
    }

    public void ClearTrackedChanges() => _db.ChangeTracker.Clear();

    public async Task<string?> ApplyAsync(SyncEventDto syncEvent, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(syncEvent.PayloadJson);
        var root = document.RootElement;
        var action = root.TryGetProperty("action", out var actionElement)
            ? actionElement.GetString() ?? "Upsert"
            : "Upsert";
        var entity = root.TryGetProperty("entity", out var entityElement) ? entityElement : default;

        return syncEvent.AggregateType switch
        {
            nameof(AccessLog) => await ApplyAccessLogAsync(syncEvent, action, entity, cancellationToken),
            nameof(Alarm) => await ApplyAlarmAsync(syncEvent, action, entity, cancellationToken),
            nameof(Visit) => await ApplyVisitAsync(syncEvent, action, entity, cancellationToken),
            nameof(Employee) => await ApplyGenericAsync<Employee>(syncEvent, action, entity, employee => employee.EmployeeId, cancellationToken,
                lookup: async fields => await FindEmployeeAsync(fields, cancellationToken)),
            nameof(Vehicle) => await ApplyGenericAsync<Vehicle>(syncEvent, action, entity, vehicle => vehicle.VehicleId, cancellationToken,
                lookup: async fields => await FindVehicleAsync(fields, cancellationToken)),
            nameof(WatchlistEntry) => await ApplyGenericAsync<WatchlistEntry>(syncEvent, action, entity, entry => entry.WatchlistEntryId, cancellationToken,
                lookup: async fields => await FindWatchlistEntryAsync(fields, cancellationToken)),
            nameof(AccessRule) => await ApplyGenericAsync<AccessRule>(syncEvent, action, entity, rule => rule.AccessRuleId, cancellationToken),
            nameof(Site) => await ApplyGenericAsync<Site>(syncEvent, action, entity, site => site.SiteId, cancellationToken,
                lookup: async fields => await FindSiteAsync(fields, cancellationToken)),
            nameof(Gate) => await ApplyGenericAsync<Gate>(syncEvent, action, entity, gate => gate.GateId, cancellationToken),
            nameof(Lane) => await ApplyGenericAsync<Lane>(syncEvent, action, entity, lane => lane.LaneId, cancellationToken),
            nameof(SecurityZone) => await ApplyGenericAsync<SecurityZone>(syncEvent, action, entity, zone => zone.SecurityZoneId, cancellationToken),
            nameof(AccessPoint) => await ApplyGenericAsync<AccessPoint>(syncEvent, action, entity, point => point.AccessPointId, cancellationToken),
            nameof(Camera) => await ApplyGenericAsync<Camera>(syncEvent, action, entity, camera => camera.CameraId, cancellationToken,
                lookup: async fields => await FindCameraAsync(fields, cancellationToken)),
            nameof(SecurityDevice) => await ApplyGenericAsync<SecurityDevice>(syncEvent, action, entity, device => device.SecurityDeviceId, cancellationToken,
                lookup: async fields => await FindSecurityDeviceAsync(fields, cancellationToken)),
            nameof(LaneEvent) => await ApplyGenericAsync<LaneEvent>(syncEvent, action, entity, laneEvent => laneEvent.LaneEventId, cancellationToken),
            nameof(ChatConversation) => await ApplyChatConversationAsync(syncEvent, action, entity, cancellationToken),
            nameof(ChatParticipant) => await ApplyChatParticipantAsync(syncEvent, action, entity, cancellationToken),
            nameof(ChatMessage) => await ApplyChatMessageAsync(syncEvent, action, entity, cancellationToken),
            nameof(RemoteFaceEnrollmentJob) => await ApplyRemoteFaceEnrollmentJobAsync(syncEvent, action, entity, cancellationToken),
            nameof(EmployeeFaceModel) => await ApplyEmployeeFaceModelAsync(syncEvent, action, entity, cancellationToken),
            _ => null
        };
    }

    private async Task<string?> ApplyRemoteFaceEnrollmentJobAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        var fields = entity.ValueKind == JsonValueKind.Object
            ? entity.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var jobId = Guid.TryParse(syncEvent.AggregateId, out var parsedGuid) ? parsedGuid : (Guid?)null;
        var existing = jobId.HasValue
            ? _db.RemoteFaceEnrollmentJobs.Local.FirstOrDefault(item => item.Id == jobId.Value)
              ?? await _db.RemoteFaceEnrollmentJobs.FirstOrDefaultAsync(item => item.Id == jobId.Value, cancellationToken)
            : null;

        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (existing != null)
            {
                _db.RemoteFaceEnrollmentJobs.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
                return existing.Id.ToString();
            }
            return syncEvent.AggregateId;
        }

        var job = existing ?? new RemoteFaceEnrollmentJob();
        ApplyScalarValues(job, entity, includePrimaryKey: existing == null);
        if (existing == null)
        {
            _db.RemoteFaceEnrollmentJobs.Add(job);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return job.Id.ToString();
    }

    private async Task<string?> ApplyEmployeeFaceModelAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        var fields = entity.ValueKind == JsonValueKind.Object
            ? entity.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var modelId = int.TryParse(syncEvent.AggregateId, out var parsedId) ? parsedId : (int?)null;
        var existing = modelId.HasValue
            ? await _db.EmployeeFaceModels.FirstOrDefaultAsync(item => item.Id == modelId.Value, cancellationToken)
            : null;

        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (existing != null)
            {
                _db.EmployeeFaceModels.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
                return existing.Id.ToString();
            }
            return syncEvent.AggregateId;
        }

        // The primary-key values are generated independently on each node, so an
        // inbound model must be matched by its business key before inserting.
        // Looking up only the active model is insufficient: archived versions
        // carry the same employee and would otherwise violate the unique index.
        var employeeId = TryGetInt(fields, nameof(EmployeeFaceModel.EmployeeId));
        var version = TryGetInt(fields, nameof(EmployeeFaceModel.Version));
        if (existing == null && employeeId.HasValue && version.HasValue)
        {
            existing = await _db.EmployeeFaceModels
                .FirstOrDefaultAsync(item => item.EmployeeId == employeeId.Value && item.Version == version.Value,
                    cancellationToken);
        }

        // Older rows may not have a version. Keep a fallback for those legacy
        // payloads, but never use it when the canonical version is available.
        if (existing == null && employeeId.HasValue && !version.HasValue)
        {
            existing = await _db.EmployeeFaceModels
                .Where(item => item.EmployeeId == employeeId.Value && item.Status == FaceModelLifecycleStatuses.Active)
                .OrderByDescending(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // The central record is canonical. Before applying an inbound active
        // version, archive any different active version that was created on
        // this node. This preserves history and satisfies the one-active-model
        // invariant without letting a stale local model block the sync queue.
        var incomingStatus = TryGetString(fields, nameof(EmployeeFaceModel.Status));
        if (employeeId.HasValue && string.Equals(incomingStatus, FaceModelLifecycleStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            var activeModelsToArchive = await _db.EmployeeFaceModels
                .Where(item => item.EmployeeId == employeeId.Value &&
                               item.Status == FaceModelLifecycleStatuses.Active &&
                               (existing == null || item.Id != existing.Id))
                .ToListAsync(cancellationToken);
            foreach (var activeModel in activeModelsToArchive)
            {
                activeModel.Status = FaceModelLifecycleStatuses.Archived;
                activeModel.ArchivedAtUtc ??= DateTime.UtcNow;
            }
        }

        var model = existing ?? new EmployeeFaceModel();
        ApplyScalarValues(model, entity, includePrimaryKey: existing == null);
        if (existing == null)
        {
            model.Id = 0;
            _db.EmployeeFaceModels.Add(model);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return model.Id.ToString();
    }

    private async Task<string?> ApplyAccessLogAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return syncEvent.AggregateId;
        }

        var accessLog = new AccessLog();
        ApplyScalarValues(accessLog, entity, includePrimaryKey: false);
        accessLog.LogId = 0;
        _db.AccessLogs.Add(accessLog);
        await _db.SaveChangesAsync(cancellationToken);
        return accessLog.LogId.ToString(CultureInfo.InvariantCulture);
    }

    private async Task<string?> ApplyAlarmAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (await TryFindMappedAlarmAsync(syncEvent, cancellationToken) is { } existingDelete)
            {
                _db.Alarms.Remove(existingDelete);
                await _db.SaveChangesAsync(cancellationToken);
                return existingDelete.AlarmId.ToString(CultureInfo.InvariantCulture);
            }

            return syncEvent.AggregateId;
        }

        var existing = await TryFindMappedAlarmAsync(syncEvent, cancellationToken);
        if (existing == null && TryGetLong(entity, nameof(Alarm.AlarmId)) is { } alarmId)
        {
            existing = await _db.Alarms.FirstOrDefaultAsync(item => item.AlarmId == alarmId, cancellationToken);
        }

        var alarm = existing ?? new Alarm();
        ApplyScalarValues(alarm, entity, includePrimaryKey: existing == null && syncEvent.SourceSystem != "AreaNode");
        if (existing == null && string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase))
        {
            alarm.AlarmId = 0;
            _db.Alarms.Add(alarm);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return alarm.AlarmId.ToString(CultureInfo.InvariantCulture);
    }

    private async Task<string?> ApplyVisitAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (await TryFindMappedVisitAsync(syncEvent, cancellationToken) is { } existingDelete)
            {
                _db.Visits.Remove(existingDelete);
                await _db.SaveChangesAsync(cancellationToken);
                return existingDelete.VisitId.ToString(CultureInfo.InvariantCulture);
            }

            return syncEvent.AggregateId;
        }

        var existing = await TryFindMappedVisitAsync(syncEvent, cancellationToken);
        if (existing == null && TryGetInt(entity, nameof(Visit.VisitId)) is { } visitId)
        {
            existing = await _db.Visits.FirstOrDefaultAsync(item => item.VisitId == visitId, cancellationToken);
        }

        var visit = existing ?? new Visit();
        ApplyScalarValues(visit, entity, includePrimaryKey: existing == null && syncEvent.SourceSystem != "AreaNode");
        if (existing == null && string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase))
        {
            visit.VisitId = 0;
            _db.Visits.Add(visit);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return visit.VisitId.ToString(CultureInfo.InvariantCulture);
    }

    private async Task<string?> ApplyGenericAsync<TEntity>(
        SyncEventDto syncEvent,
        string action,
        JsonElement entity,
        Func<TEntity, object?> primaryKeySelector,
        CancellationToken cancellationToken,
        Func<Dictionary<string, JsonElement>, Task<TEntity?>>? lookup = null)
        where TEntity : class, new()
    {
        var fields = entity.ValueKind == JsonValueKind.Object
            ? entity.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var existing = lookup != null ? await lookup(fields) : null;
        if (existing == null && syncEvent.AggregateId != null)
        {
            var intKey = int.TryParse(syncEvent.AggregateId, out var parsedInt) ? parsedInt : (int?)null;
            if (intKey.HasValue)
            {
                existing = await _db.Set<TEntity>().FindAsync([intKey.Value], cancellationToken);
            }
        }

        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (existing != null)
            {
                _db.Set<TEntity>().Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
                return primaryKeySelector(existing)?.ToString();
            }

            return syncEvent.AggregateId;
        }

        var entityInstance = existing ?? new TEntity();
        ApplyScalarValues(entityInstance, entity, includePrimaryKey: existing == null);
        if (existing == null)
        {
            var isCentralInsert = string.Equals(syncEvent.SourceSystem, "Central", StringComparison.OrdinalIgnoreCase);
            if (isCentralInsert)
            {
                await SaveWithIdentityInsertAsync<TEntity>(async () => await SaveEntityWithIdentityInsertAsync(entityInstance, cancellationToken), cancellationToken);
                return primaryKeySelector(entityInstance)?.ToString();
            }

            NormalizeGeneratedPrimaryKeyForInsert(entityInstance, syncEvent);
            _db.Set<TEntity>().Add(entityInstance);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return primaryKeySelector(entityInstance)?.ToString();
    }

    private async Task SaveEntityWithIdentityInsertAsync<TEntity>(TEntity entityInstance, CancellationToken cancellationToken) where TEntity : class
    {
        _db.Set<TEntity>().Add(entityInstance);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveWithIdentityInsertAsync<TEntity>(Func<Task> saveAction, CancellationToken cancellationToken) where TEntity : class
    {
        var entityType = _db.Model.FindEntityType(typeof(TEntity));
        var tableName = entityType?.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            await saveAction();
            return;
        }

        await _db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await _db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] ON", cancellationToken);
            await saveAction();
            await _db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] OFF", cancellationToken);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private void NormalizeGeneratedPrimaryKeyForInsert<TEntity>(TEntity entityInstance, SyncEventDto syncEvent, bool forceResetGeneratedKey = false) where TEntity : class
    {
        var entityType = _db.Model.FindEntityType(typeof(TEntity));
        var primaryKey = entityType?.FindPrimaryKey();
        if (primaryKey == null || primaryKey.Properties.Count != 1)
        {
            return;
        }

        var keyProperty = primaryKey.Properties[0];
        if (keyProperty.ValueGenerated != ValueGenerated.OnAdd)
        {
            return;
        }

        var propertyInfo = typeof(TEntity).GetProperty(keyProperty.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (propertyInfo == null || !propertyInfo.CanWrite)
        {
            return;
        }

        var currentValue = propertyInfo.GetValue(entityInstance);
        var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
        var shouldResetForAreaNode = string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase);
        var shouldResetGeneratedKey = forceResetGeneratedKey || shouldResetForAreaNode;

        if (propertyType == typeof(int) && currentValue is int intValue && (shouldResetGeneratedKey || intValue <= 0))
        {
            propertyInfo.SetValue(entityInstance, 0);
            return;
        }

        if (propertyType == typeof(long) && currentValue is long longValue && (shouldResetGeneratedKey || longValue <= 0))
        {
            propertyInfo.SetValue(entityInstance, 0L);
        }
    }

    private async Task<Alarm?> TryFindMappedAlarmAsync(SyncEventDto syncEvent, CancellationToken cancellationToken)
    {
        if (!string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(syncEvent.AreaNodeId))
        {
            return null;
        }

        var mappedId = await _db.SyncInboundEvents
            .Where(item => item.AreaNodeId == syncEvent.AreaNodeId &&
                           item.AggregateType == nameof(Alarm) &&
                           item.AggregateId == syncEvent.AggregateId &&
                           item.AppliedAggregateId != null)
            .OrderByDescending(item => item.SyncInboundEventId)
            .Select(item => item.AppliedAggregateId)
            .FirstOrDefaultAsync(cancellationToken);

        return long.TryParse(mappedId, out var parsedId)
            ? await _db.Alarms.FirstOrDefaultAsync(item => item.AlarmId == parsedId, cancellationToken)
            : null;
    }

    private async Task<Visit?> TryFindMappedVisitAsync(SyncEventDto syncEvent, CancellationToken cancellationToken)
    {
        if (!string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(syncEvent.AreaNodeId))
        {
            return null;
        }

        var mappedId = await _db.SyncInboundEvents
            .Where(item => item.AreaNodeId == syncEvent.AreaNodeId &&
                           item.AggregateType == nameof(Visit) &&
                           item.AggregateId == syncEvent.AggregateId &&
                           item.AppliedAggregateId != null)
            .OrderByDescending(item => item.SyncInboundEventId)
            .Select(item => item.AppliedAggregateId)
            .FirstOrDefaultAsync(cancellationToken);

        return int.TryParse(mappedId, out var parsedId)
            ? await _db.Visits.FirstOrDefaultAsync(item => item.VisitId == parsedId, cancellationToken)
            : null;
    }

    private async Task<Employee?> FindEmployeeAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(Employee.EmployeeId)) is { } employeeId)
        {
            var byId = await _db.Employees.FirstOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
            if (byId != null) return byId;
        }

        var email = TryGetString(fields, nameof(Employee.Email));
        if (!string.IsNullOrWhiteSpace(email))
        {
            var byEmail = await _db.Employees.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
            if (byEmail != null) return byEmail;
        }

        return null;
    }

    private async Task<Vehicle?> FindVehicleAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(Vehicle.VehicleId)) is { } vehicleId)
        {
            var byId = await _db.Vehicles.FirstOrDefaultAsync(item => item.VehicleId == vehicleId, cancellationToken);
            if (byId != null) return byId;
        }

        var plate = TryGetString(fields, nameof(Vehicle.LicensePlate));
        if (!string.IsNullOrWhiteSpace(plate))
        {
            return await _db.Vehicles.FirstOrDefaultAsync(item => item.LicensePlate == plate, cancellationToken);
        }

        return null;
    }

    private async Task<WatchlistEntry?> FindWatchlistEntryAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(WatchlistEntry.WatchlistEntryId)) is { } watchlistId)
        {
            var byId = await _db.WatchlistEntries.FirstOrDefaultAsync(item => item.WatchlistEntryId == watchlistId, cancellationToken);
            if (byId != null) return byId;
        }

        var identifier = TryGetString(fields, nameof(WatchlistEntry.Identifier));
        var displayName = TryGetString(fields, nameof(WatchlistEntry.DisplayName));
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            var byIdentifier = await _db.WatchlistEntries.FirstOrDefaultAsync(item =>
                item.Identifier == identifier && item.DisplayName == displayName, cancellationToken);
            if (byIdentifier != null) return byIdentifier;
        }

        return null;
    }

    private async Task<Site?> FindSiteAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(Site.SiteId)) is { } siteId)
        {
            var byId = await _db.Sites.FirstOrDefaultAsync(item => item.SiteId == siteId, cancellationToken);
            if (byId != null) return byId;
        }

        var code = TryGetString(fields, nameof(Site.Code));
        var companyId = TryGetInt(fields, nameof(Site.CompanyId));
        if (!string.IsNullOrWhiteSpace(code) && companyId.HasValue)
        {
            return await _db.Sites.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Code == code, cancellationToken);
        }

        return null;
    }

    private async Task<Camera?> FindCameraAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(Camera.CameraId)) is { } cameraId)
        {
            var byId = await _db.Cameras.FirstOrDefaultAsync(item => item.CameraId == cameraId, cancellationToken);
            if (byId != null) return byId;
        }

        var streamUrl = TryGetString(fields, nameof(Camera.StreamUrl));
        if (!string.IsNullOrWhiteSpace(streamUrl))
        {
            var byStream = await _db.Cameras.FirstOrDefaultAsync(item => item.StreamUrl == streamUrl, cancellationToken);
            if (byStream != null) return byStream;
        }

        var cameraName = TryGetString(fields, nameof(Camera.CameraName));
        var gateId = TryGetInt(fields, nameof(Camera.GateId));
        if (!string.IsNullOrWhiteSpace(cameraName))
        {
            return await _db.Cameras.FirstOrDefaultAsync(item => item.CameraName == cameraName && item.GateId == gateId, cancellationToken);
        }

        return null;
    }

    private async Task<SecurityDevice?> FindSecurityDeviceAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(SecurityDevice.SecurityDeviceId)) is { } deviceId)
        {
            var byId = await _db.SecurityDevices.FirstOrDefaultAsync(item => item.SecurityDeviceId == deviceId, cancellationToken);
            if (byId != null) return byId;
        }

        var serialNumber = TryGetString(fields, nameof(SecurityDevice.SerialNumber));
        if (!string.IsNullOrWhiteSpace(serialNumber))
        {
            var bySerial = await _db.SecurityDevices.FirstOrDefaultAsync(item => item.SerialNumber == serialNumber, cancellationToken);
            if (bySerial != null) return bySerial;
        }

        var name = TryGetString(fields, nameof(SecurityDevice.Name));
        var siteId = TryGetInt(fields, nameof(SecurityDevice.SiteId));
        if (!string.IsNullOrWhiteSpace(name))
        {
            return await _db.SecurityDevices.FirstOrDefaultAsync(item => item.Name == name && item.SiteId == siteId, cancellationToken);
        }

        return null;
    }

    private async Task<string?> ApplyChatConversationAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        var fields = entity.ValueKind == JsonValueKind.Object
            ? entity.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var existing = await FindChatConversationAsync(fields, cancellationToken);
        if (existing == null &&
            string.Equals(syncEvent.SourceSystem, "Central", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(syncEvent.AggregateId) &&
            await TryMapCentralConversationIdAsync(syncEvent.AggregateId, cancellationToken) is { } mappedConversationId)
        {
            existing = await _db.ChatConversations.FirstOrDefaultAsync(
                item => item.ConversationId == mappedConversationId,
                cancellationToken);
        }

        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (existing != null)
            {
                _db.ChatConversations.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
                return existing.ConversationId.ToString(CultureInfo.InvariantCulture);
            }

            return syncEvent.AggregateId;
        }

        var conversation = existing ?? new ChatConversation();
        ApplyScalarValues(conversation, entity, includePrimaryKey: existing == null);
        if (existing == null)
        {
            NormalizeGeneratedPrimaryKeyForInsert(
                conversation,
                syncEvent,
                forceResetGeneratedKey: !string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase));
            _db.ChatConversations.Add(conversation);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await SaveCentralChatConversationMappingAsync(syncEvent, conversation.ConversationId, cancellationToken);
        return conversation.ConversationId.ToString(CultureInfo.InvariantCulture);
    }

    private async Task<ChatConversation?> FindChatConversationAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(ChatConversation.ConversationId)) is { } conversationId)
        {
            var byId = await _db.ChatConversations.FirstOrDefaultAsync(item => item.ConversationId == conversationId, cancellationToken);
            if (byId != null) return byId;
        }

        var title = TryGetString(fields, nameof(ChatConversation.Title));
        var createdAt = TryGetDateTime(fields, nameof(ChatConversation.CreatedAt));
        if (createdAt.HasValue)
        {
            var query = _db.ChatConversations
                .Where(item => item.CreatedAt == createdAt.Value);

            if (!string.IsNullOrWhiteSpace(title))
            {
                query = query.Where(item => item.Title == title);
            }

            return await query
                .OrderBy(item => item.ConversationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<ChatParticipant?> FindChatParticipantAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(ChatParticipant.ParticipantId)) is { } participantId)
        {
            var byId = await _db.ChatParticipants.FirstOrDefaultAsync(item => item.ParticipantId == participantId, cancellationToken);
            if (byId != null) return byId;
        }

        var conversationId = TryGetInt(fields, nameof(ChatParticipant.ConversationId));
        var employeeId = TryGetInt(fields, nameof(ChatParticipant.EmployeeId));
        if (conversationId.HasValue && employeeId.HasValue)
        {
            return await _db.ChatParticipants.FirstOrDefaultAsync(item =>
                item.ConversationId == conversationId.Value &&
                item.EmployeeId == employeeId.Value, cancellationToken);
        }

        return null;
    }

    private async Task<string?> ApplyChatParticipantAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        var fields = entity.ValueKind == JsonValueKind.Object
            ? entity.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var existing = await FindChatParticipantAsync(fields, cancellationToken);
        if (existing == null && syncEvent.AggregateId != null && int.TryParse(syncEvent.AggregateId, out var aggregateId))
        {
            existing = await _db.ChatParticipants.FirstOrDefaultAsync(item => item.ParticipantId == aggregateId, cancellationToken);
        }

        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (existing != null)
            {
                _db.ChatParticipants.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
                return existing.ParticipantId.ToString(CultureInfo.InvariantCulture);
            }

            return syncEvent.AggregateId;
        }

        var participant = existing ?? new ChatParticipant();
        ApplyScalarValues(participant, entity, includePrimaryKey: existing == null);
        await RemapAreaNodeChatReferencesAsync(participant, syncEvent, cancellationToken);
        existing ??= await FindExistingChatParticipantByNormalizedKeysAsync(participant, cancellationToken);
        if (existing != null && !ReferenceEquals(existing, participant))
        {
            participant = existing;
            ApplyScalarValues(participant, entity, includePrimaryKey: false);
            await RemapAreaNodeChatReferencesAsync(participant, syncEvent, cancellationToken);
        }

        if (existing == null)
        {
            NormalizeGeneratedPrimaryKeyForInsert(
                participant,
                syncEvent,
                forceResetGeneratedKey: !string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase));
            if (!await ConversationExistsAsync(participant.ConversationId, cancellationToken))
            {
                // Conversation cha chưa tồn tại (event đến lệch thứ tự) -> bỏ qua, tránh FK violation.
                return null;
            }
            _db.ChatParticipants.Add(participant);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return participant.ParticipantId.ToString(CultureInfo.InvariantCulture);
    }

    private async Task<bool> ConversationExistsAsync(int conversationId, CancellationToken cancellationToken)
    {
        if (conversationId <= 0)
        {
            return false;
        }

        return await _db.ChatConversations.AnyAsync(item => item.ConversationId == conversationId, cancellationToken);
    }

    private async Task<ChatMessage?> FindChatMessageAsync(Dictionary<string, JsonElement> fields, CancellationToken cancellationToken)
    {
        if (TryGetInt(fields, nameof(ChatMessage.MessageId)) is { } messageId)
        {
            var byId = await _db.ChatMessages.FirstOrDefaultAsync(item => item.MessageId == messageId, cancellationToken);
            if (byId != null) return byId;
        }

        var conversationId = TryGetInt(fields, nameof(ChatMessage.ConversationId));
        var senderId = TryGetInt(fields, nameof(ChatMessage.SenderId));
        var clientMessageId = TryGetString(fields, nameof(ChatMessage.ClientMessageId));
        if (conversationId.HasValue && senderId.HasValue && !string.IsNullOrWhiteSpace(clientMessageId))
        {
            var byClientId = await _db.ChatMessages.FirstOrDefaultAsync(item =>
                item.ConversationId == conversationId.Value &&
                item.SenderId == senderId.Value &&
                item.ClientMessageId == clientMessageId, cancellationToken);
            if (byClientId != null) return byClientId;
        }

        var sentAt = TryGetDateTime(fields, nameof(ChatMessage.SentAt));
        var content = TryGetString(fields, nameof(ChatMessage.Content));
        var messageType = TryGetString(fields, nameof(ChatMessage.MessageType));
        if (conversationId.HasValue && senderId.HasValue && sentAt.HasValue && !string.IsNullOrWhiteSpace(content))
        {
            return await _db.ChatMessages.FirstOrDefaultAsync(item =>
                item.ConversationId == conversationId.Value &&
                item.SenderId == senderId.Value &&
                item.SentAt == sentAt.Value &&
                item.Content == content &&
                item.MessageType == (string.IsNullOrWhiteSpace(messageType) ? "Text" : messageType), cancellationToken);
        }

        return null;
    }

    private async Task<string?> ApplyChatMessageAsync(SyncEventDto syncEvent, string action, JsonElement entity, CancellationToken cancellationToken)
    {
        var fields = entity.ValueKind == JsonValueKind.Object
            ? entity.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var existing = await FindChatMessageAsync(fields, cancellationToken);
        if (existing == null && syncEvent.AggregateId != null && int.TryParse(syncEvent.AggregateId, out var aggregateId))
        {
            existing = await _db.ChatMessages.FirstOrDefaultAsync(item => item.MessageId == aggregateId, cancellationToken);
        }

        if (string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            if (existing != null)
            {
                _db.ChatMessages.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken);
                return existing.MessageId.ToString(CultureInfo.InvariantCulture);
            }

            return syncEvent.AggregateId;
        }

        var message = existing ?? new ChatMessage();
        ApplyScalarValues(message, entity, includePrimaryKey: existing == null);
        await RemapAreaNodeChatReferencesAsync(message, syncEvent, cancellationToken);
        existing ??= await FindExistingChatMessageByNormalizedKeysAsync(message, cancellationToken);
        if (existing != null && !ReferenceEquals(existing, message))
        {
            message = existing;
            ApplyScalarValues(message, entity, includePrimaryKey: false);
            await RemapAreaNodeChatReferencesAsync(message, syncEvent, cancellationToken);
        }

        if (existing == null)
        {
            NormalizeGeneratedPrimaryKeyForInsert(
                message,
                syncEvent,
                forceResetGeneratedKey: !string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase));
            if (!await ConversationExistsAsync(message.ConversationId, cancellationToken))
            {
                // Conversation cha chưa tồn tại (event đến lệch thứ tự) -> bỏ qua, tránh FK violation.
                return null;
            }
            _db.ChatMessages.Add(message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return message.MessageId.ToString(CultureInfo.InvariantCulture);
    }

    private static void ApplyScalarValues(object target, JsonElement entity, bool includePrimaryKey)
    {
        if (entity.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var targetType = target.GetType();
        var primaryKeyNames = targetType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() != null)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var jsonProperty in entity.EnumerateObject())
        {
            var property = targetType.GetProperty(jsonProperty.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null || !property.CanWrite)
            {
                continue;
            }

            if (!includePrimaryKey && primaryKeyNames.Contains(property.Name))
            {
                continue;
            }

            if (!IsScalarProperty(property.PropertyType))
            {
                continue;
            }

            var converted = ConvertJsonValue(jsonProperty.Value, property.PropertyType);
            property.SetValue(target, converted);
        }
    }

    private static bool IsScalarProperty(Type propertyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return underlyingType.IsPrimitive ||
               underlyingType.IsEnum ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(Guid) ||
               underlyingType == typeof(bool);
    }

    private static object? ConvertJsonValue(JsonElement value, Type targetType)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlyingType == typeof(string))
        {
            return value.GetString();
        }

        if (underlyingType == typeof(int))
        {
            return value.ValueKind == JsonValueKind.Number ? value.GetInt32() : int.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        if (underlyingType == typeof(long))
        {
            return value.ValueKind == JsonValueKind.Number ? value.GetInt64() : long.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        if (underlyingType == typeof(bool))
        {
            return value.ValueKind == JsonValueKind.True || (value.ValueKind != JsonValueKind.False && bool.Parse(value.ToString()));
        }

        if (underlyingType == typeof(decimal))
        {
            return value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : decimal.Parse(value.ToString(), CultureInfo.InvariantCulture);
        }

        if (underlyingType == typeof(DateTime))
        {
            return value.ValueKind == JsonValueKind.String
                ? DateTime.Parse(value.GetString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                : value.GetDateTime();
        }

        if (underlyingType == typeof(Guid))
        {
            return Guid.Parse(value.GetString() ?? string.Empty);
        }

        if (underlyingType.IsEnum)
        {
            return Enum.Parse(underlyingType, value.GetString() ?? value.ToString(), true);
        }

        return null;
    }

    private async Task RemapAreaNodeChatReferencesAsync(ChatParticipant participant, SyncEventDto syncEvent, CancellationToken cancellationToken)
    {
        if (!string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(syncEvent.AreaNodeId))
        {
            await RemapCentralChatReferencesAsync(participant, syncEvent, cancellationToken);
            return;
        }

        var mappedConversationId = await TryMapAreaNodeAggregateIdAsync(
            syncEvent.AreaNodeId,
            nameof(ChatConversation),
            participant.ConversationId.ToString(CultureInfo.InvariantCulture),
            cancellationToken);

        if (mappedConversationId.HasValue)
        {
            participant.ConversationId = mappedConversationId.Value;
        }
    }

    private async Task RemapAreaNodeChatReferencesAsync(ChatMessage message, SyncEventDto syncEvent, CancellationToken cancellationToken)
    {
        if (!string.Equals(syncEvent.SourceSystem, "AreaNode", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(syncEvent.AreaNodeId))
        {
            await RemapCentralChatReferencesAsync(message, syncEvent, cancellationToken);
            return;
        }

        var mappedConversationId = await TryMapAreaNodeAggregateIdAsync(
            syncEvent.AreaNodeId,
            nameof(ChatConversation),
            message.ConversationId.ToString(CultureInfo.InvariantCulture),
            cancellationToken);

        if (mappedConversationId.HasValue)
        {
            message.ConversationId = mappedConversationId.Value;
        }
    }

    private async Task RemapCentralChatReferencesAsync(ChatParticipant participant, SyncEventDto syncEvent, CancellationToken cancellationToken)
    {
        if (!string.Equals(syncEvent.SourceSystem, "Central", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var mappedConversationId = await TryMapCentralConversationIdAsync(participant.ConversationId, cancellationToken);
        if (mappedConversationId.HasValue)
        {
            participant.ConversationId = mappedConversationId.Value;
        }
    }

    private async Task RemapCentralChatReferencesAsync(ChatMessage message, SyncEventDto syncEvent, CancellationToken cancellationToken)
    {
        if (!string.Equals(syncEvent.SourceSystem, "Central", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var mappedConversationId = await TryMapCentralConversationIdAsync(message.ConversationId, cancellationToken);
        if (mappedConversationId.HasValue)
        {
            message.ConversationId = mappedConversationId.Value;
        }
    }

    private async Task<ChatParticipant?> FindExistingChatParticipantByNormalizedKeysAsync(ChatParticipant participant, CancellationToken cancellationToken)
    {
        return await _db.ChatParticipants.FirstOrDefaultAsync(item =>
            item.ConversationId == participant.ConversationId &&
            item.EmployeeId == participant.EmployeeId, cancellationToken);
    }

    private async Task<ChatMessage?> FindExistingChatMessageByNormalizedKeysAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(message.ClientMessageId))
        {
            var byClientMessage = await _db.ChatMessages.FirstOrDefaultAsync(item =>
                item.ConversationId == message.ConversationId &&
                item.SenderId == message.SenderId &&
                item.ClientMessageId == message.ClientMessageId, cancellationToken);
            if (byClientMessage != null)
            {
                return byClientMessage;
            }
        }

        return await _db.ChatMessages.FirstOrDefaultAsync(item =>
            item.ConversationId == message.ConversationId &&
            item.SenderId == message.SenderId &&
            item.SentAt == message.SentAt &&
            item.Content == message.Content &&
            item.MessageType == message.MessageType, cancellationToken);
    }

    private async Task SaveCentralChatConversationMappingAsync(SyncEventDto syncEvent, int localConversationId, CancellationToken cancellationToken)
    {
        if (!string.Equals(syncEvent.SourceSystem, "Central", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(syncEvent.AggregateId))
        {
            return;
        }

        var key = BuildCentralConversationMappingKey(syncEvent.AggregateId);
        var entry = await _db.SystemConfigs.FindAsync([key], cancellationToken);
        if (entry == null)
        {
            entry = new SystemConfig { Key = key };
            _db.SystemConfigs.Add(entry);
        }

        entry.Value = localConversationId.ToString(CultureInfo.InvariantCulture);
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int?> TryMapCentralConversationIdAsync(int conversationId, CancellationToken cancellationToken) =>
        await TryMapCentralConversationIdAsync(conversationId.ToString(CultureInfo.InvariantCulture), cancellationToken);

    private async Task<int?> TryMapCentralConversationIdAsync(string conversationId, CancellationToken cancellationToken)
    {
        var key = BuildCentralConversationMappingKey(conversationId);
        var mappedId = await _db.SystemConfigs
            .Where(item => item.Key == key)
            .Select(item => item.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return int.TryParse(mappedId, out var parsedId) ? parsedId : null;
    }

    private static string BuildCentralConversationMappingKey(string aggregateId) =>
        $"sync.chatconversation.map.central.{aggregateId}";

    private async Task<int?> TryMapAreaNodeAggregateIdAsync(
        string areaNodeId,
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken)
    {
        var mappedId = await _db.SyncInboundEvents
            .Where(item => item.AreaNodeId == areaNodeId &&
                           item.AggregateType == aggregateType &&
                           item.AggregateId == aggregateId &&
                           item.AppliedAggregateId != null)
            .OrderByDescending(item => item.SyncInboundEventId)
            .Select(item => item.AppliedAggregateId)
            .FirstOrDefaultAsync(cancellationToken);

        return int.TryParse(mappedId, out var parsedId) ? parsedId : null;
    }

    private static int? TryGetInt(JsonElement entity, string propertyName)
    {
        if (entity.ValueKind != JsonValueKind.Object || !entity.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : int.TryParse(property.ToString(), out var parsed)
                ? parsed
                : null;
    }

    private static long? TryGetLong(JsonElement entity, string propertyName)
    {
        if (entity.ValueKind != JsonValueKind.Object || !entity.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number
            ? property.TryGetInt64(out var value) ? value : null
            : long.TryParse(property.ToString(), out var parsed)
                ? parsed
                : null;
    }

    private static int? TryGetInt(Dictionary<string, JsonElement> values, string propertyName)
    {
        return values.TryGetValue(propertyName, out var property)
            ? property.ValueKind == JsonValueKind.Number
                ? property.GetInt32()
                : int.TryParse(property.ToString(), out var parsed)
                    ? parsed
                    : null
            : null;
    }

    private static string? TryGetString(Dictionary<string, JsonElement> values, string propertyName)
    {
        return values.TryGetValue(propertyName, out var property) ? property.ToString() : null;
    }

    private static DateTime? TryGetDateTime(Dictionary<string, JsonElement> values, string propertyName)
    {
        if (!values.TryGetValue(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(property.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return property.ValueKind == JsonValueKind.String || property.ValueKind == JsonValueKind.Number
            ? DateTime.TryParse(property.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed)
                ? parsed
                : null
            : null;
    }
}
