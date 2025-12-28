using EMR.Domain.Common;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Serializes and deserializes domain events to/from JSON.
/// Handles event versioning and type resolution.
/// </summary>
public class EventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, Type> _eventTypeCache;

    public EventSerializer()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        _eventTypeCache = new Dictionary<string, Type>();
        LoadEventTypes();
    }

    /// <summary>
    /// Serializes a domain event to JSON string.
    /// </summary>
    public string Serialize(IDomainEvent domainEvent)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions);
    }

    /// <summary>
    /// Deserializes a domain event from JSON string.
    /// </summary>
    public IDomainEvent Deserialize(string eventData, string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventData))
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentNullException(nameof(eventType));
        }

        var type = GetEventType(eventType);
        if (type == null)
        {
            throw new InvalidOperationException($"Event type '{eventType}' not found in loaded assemblies.");
        }

        var domainEvent = JsonSerializer.Deserialize(eventData, type, _jsonOptions) as IDomainEvent;
        if (domainEvent == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event of type '{eventType}'.");
        }

        return domainEvent;
    }

    /// <summary>
    /// Deserializes a domain event to a specific type.
    /// </summary>
    public TEvent Deserialize<TEvent>(string eventData) where TEvent : IDomainEvent
    {
        if (string.IsNullOrWhiteSpace(eventData))
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        var domainEvent = JsonSerializer.Deserialize<TEvent>(eventData, _jsonOptions);
        if (domainEvent == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event of type '{typeof(TEvent).FullName}'.");
        }

        return domainEvent;
    }

    /// <summary>
    /// Gets the event type from the type name.
    /// Uses caching for performance.
    /// </summary>
    private Type? GetEventType(string eventType)
    {
        if (_eventTypeCache.TryGetValue(eventType, out var cachedType))
        {
            return cachedType;
        }

        var type = Type.GetType(eventType);
        if (type != null)
        {
            _eventTypeCache[eventType] = type;
        }

        return type;
    }

    /// <summary>
    /// Loads all event types from the domain assembly.
    /// This is called once during initialization for performance.
    /// </summary>
    private void LoadEventTypes()
    {
        var domainAssembly = typeof(IDomainEvent).Assembly;
        var eventTypes = domainAssembly.GetTypes()
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var eventType in eventTypes)
        {
            var typeName = eventType.FullName ?? eventType.Name;
            _eventTypeCache[typeName] = eventType;
        }
    }
}

/// <summary>
/// Interface for event serialization.
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serializes a domain event to JSON.
    /// </summary>
    string Serialize(IDomainEvent domainEvent);

    /// <summary>
    /// Deserializes a domain event from JSON.
    /// </summary>
    IDomainEvent Deserialize(string eventData, string eventType);

    /// <summary>
    /// Deserializes a domain event to a specific type.
    /// </summary>
    TEvent Deserialize<TEvent>(string eventData) where TEvent : IDomainEvent;
}
