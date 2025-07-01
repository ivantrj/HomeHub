using HomeHub.Api.Models;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace HomeHub.Api.Services;

public interface IHueService
{
    Task<List<HueBridge>> DiscoverBridgesAsync();
    Task<string> CreateUserAsync(string bridgeIpAddress);
    Task<List<HueLight>> GetLightsAsync();
    Task<HueLight?> GetLightAsync(string lightId);
    Task<bool> SetLightStateAsync(string lightId, HueLightState state);
    Task<bool> ToggleLightAsync(string lightId);
    void SetBridgeConfig(string ipAddress, string username);
}

public class HueService : IHueService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HueService> _logger;
    private HueBridge? _bridge;

    public HueService(HttpClient httpClient, ILogger<HueService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void SetBridgeConfig(string ipAddress, string username)
    {
        _bridge = new HueBridge
        {
            IpAddress = ipAddress,
            Username = username,
            IsConnected = !string.IsNullOrEmpty(ipAddress) && !string.IsNullOrEmpty(username)
        };
    }

    public async Task<List<HueBridge>> DiscoverBridgesAsync()
    {
        try
        {
            // Try to discover bridges using the official Philips discovery service
            var response = await _httpClient.GetStringAsync("https://discovery.meethue.com/");
            var bridges = JsonConvert.DeserializeObject<List<dynamic>>(response);
            
            var result = new List<HueBridge>();
            if (bridges != null)
            {
                foreach (var bridge in bridges)
                {
                    result.Add(new HueBridge
                    {
                        IpAddress = bridge.internalipaddress,
                        BridgeId = bridge.id
                    });
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Hue bridges");
            return new List<HueBridge>();
        }
    }

    public async Task<string> CreateUserAsync(string bridgeIpAddress)
    {
        try
        {
            var requestBody = JsonConvert.SerializeObject(new { devicetype = "HomeHub#HomeHubApp" });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"http://{bridgeIpAddress}/api", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonConvert.DeserializeObject<dynamic[]>(responseContent);
            if (result != null && result.Length > 0)
            {
                if (result[0].success?.username != null)
                {
                    return result[0].success.username;
                }
                else if (result[0].error != null)
                {
                    throw new Exception($"Hue Bridge Error: {result[0].error.description}");
                }
            }
            
            throw new Exception("Failed to create user on Hue bridge");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user on Hue bridge");
            throw;
        }
    }

    public async Task<List<HueLight>> GetLightsAsync()
    {
        if (_bridge == null || !_bridge.IsConnected)
            throw new InvalidOperationException("Hue bridge not configured");

        try
        {
            var response = await _httpClient.GetStringAsync($"http://{_bridge.IpAddress}/api/{_bridge.Username}/lights");
            var lightsData = JsonConvert.DeserializeObject<Dictionary<string, HueLightInfo>>(response);
            
            var lights = new List<HueLight>();
            if (lightsData != null)
            {
                foreach (var kvp in lightsData)
                {
                    lights.Add(ConvertToHueLight(kvp.Key, kvp.Value));
                }
            }
            
            return lights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lights from Hue bridge");
            throw;
        }
    }

    public async Task<HueLight?> GetLightAsync(string lightId)
    {
        if (_bridge == null || !_bridge.IsConnected)
            throw new InvalidOperationException("Hue bridge not configured");

        try
        {
            var response = await _httpClient.GetStringAsync($"http://{_bridge.IpAddress}/api/{_bridge.Username}/lights/{lightId}");
            var lightData = JsonConvert.DeserializeObject<HueLightInfo>(response);
            
            return lightData != null ? ConvertToHueLight(lightId, lightData) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get light {LightId} from Hue bridge", lightId);
            return null;
        }
    }

    public async Task<bool> SetLightStateAsync(string lightId, HueLightState state)
    {
        if (_bridge == null || !_bridge.IsConnected)
            throw new InvalidOperationException("Hue bridge not configured");

        try
        {
            var requestBody = JsonConvert.SerializeObject(state);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"http://{_bridge.IpAddress}/api/{_bridge.Username}/lights/{lightId}/state", content);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set light state for light {LightId}", lightId);
            return false;
        }
    }

    public async Task<bool> ToggleLightAsync(string lightId)
    {
        var light = await GetLightAsync(lightId);
        if (light == null) return false;

        var newState = new HueLightState { On = !light.IsOn };
        return await SetLightStateAsync(lightId, newState);
    }

    private static HueLight ConvertToHueLight(string id, HueLightInfo lightInfo)
    {
        return new HueLight
        {
            Id = id,
            Name = lightInfo.Name,
            IsOn = lightInfo.State.On,
            IsOnline = lightInfo.State.Reachable ?? true,
            Brightness = lightInfo.State.Brightness ?? 254,
            Hue = lightInfo.State.Hue ?? 0,
            Saturation = lightInfo.State.Saturation ?? 0,
            ColorMode = lightInfo.State.ColorMode ?? "xy",
            XY = lightInfo.State.XY ?? [0.3, 0.3],
            ColorTemperature = lightInfo.State.ColorTemperature ?? 153,
            Reachable = lightInfo.State.Reachable ?? true,
            LastUpdated = DateTime.UtcNow
        };
    }
} 