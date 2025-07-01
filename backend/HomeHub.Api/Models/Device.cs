namespace HomeHub.Api.Models;

public abstract class Device
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceType Type { get; set; }
    public bool IsOnline { get; set; }
    public bool IsOn { get; set; }
    public DateTime LastUpdated { get; set; }
}

public enum DeviceType
{
    Light,
    SmartPlug,
    AlexaRoutine
}

public class DeviceStatus
{
    public string DeviceId { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class DeviceCommand
{
    public string DeviceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
} 