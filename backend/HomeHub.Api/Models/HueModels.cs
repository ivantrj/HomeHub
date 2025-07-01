using Newtonsoft.Json;

namespace HomeHub.Api.Models;

public class HueLight : Device
{
    public int Brightness { get; set; } = 254; // 0-254
    public int Hue { get; set; } = 0; // 0-65535
    public int Saturation { get; set; } = 0; // 0-254
    public string ColorMode { get; set; } = "xy";
    public double[] XY { get; set; } = [0.3, 0.3];
    public int ColorTemperature { get; set; } = 153; // 153-500 mireds
    public bool Reachable { get; set; } = true;

    public HueLight()
    {
        Type = DeviceType.Light;
    }
}

public class HueBridge
{
    public string IpAddress { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string BridgeId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}

public class HueLightState
{
    [JsonProperty("on")]
    public bool On { get; set; }

    [JsonProperty("bri")]
    public int? Brightness { get; set; }

    [JsonProperty("hue")]
    public int? Hue { get; set; }

    [JsonProperty("sat")]
    public int? Saturation { get; set; }

    [JsonProperty("xy")]
    public double[]? XY { get; set; }

    [JsonProperty("ct")]
    public int? ColorTemperature { get; set; }

    [JsonProperty("alert")]
    public string Alert { get; set; } = "none";

    [JsonProperty("effect")]
    public string Effect { get; set; } = "none";

    [JsonProperty("colormode")]
    public string? ColorMode { get; set; }

    [JsonProperty("reachable")]
    public bool? Reachable { get; set; }
}

public class HueLightInfo
{
    [JsonProperty("state")]
    public HueLightState State { get; set; } = new();

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("modelid")]
    public string ModelId { get; set; } = string.Empty;

    [JsonProperty("manufacturername")]
    public string ManufacturerName { get; set; } = string.Empty;

    [JsonProperty("productname")]
    public string ProductName { get; set; } = string.Empty;

    [JsonProperty("uniqueid")]
    public string UniqueId { get; set; } = string.Empty;
} 