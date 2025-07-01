using HomeHub.Api.Models;
using HomeHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IHueService _hueService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IHueService hueService, ILogger<DevicesController> logger)
    {
        _hueService = hueService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Device>>> GetAllDevices()
    {
        try
        {
            var devices = new List<Device>();
            
            // Get Hue lights
            var hueLights = await _hueService.GetLightsAsync();
            devices.AddRange(hueLights);
            
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get devices");
            return StatusCode(500, "Failed to retrieve devices");
        }
    }

    [HttpGet("{deviceId}")]
    public async Task<ActionResult<Device>> GetDevice(string deviceId)
    {
        try
        {
            // Try to get as Hue light first
            var hueLight = await _hueService.GetLightAsync(deviceId);
            if (hueLight != null)
                return Ok(hueLight);
            
            return NotFound($"Device with ID {deviceId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device {DeviceId}", deviceId);
            return StatusCode(500, "Failed to retrieve device");
        }
    }

    [HttpPost("{deviceId}/toggle")]
    public async Task<ActionResult> ToggleDevice(string deviceId)
    {
        try
        {
            // Try to toggle as Hue light
            var success = await _hueService.ToggleLightAsync(deviceId);
            if (success)
                return Ok(new { message = "Device toggled successfully" });
            
            return BadRequest("Failed to toggle device");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle device {DeviceId}", deviceId);
            return StatusCode(500, "Failed to toggle device");
        }
    }

    [HttpPost("{deviceId}/command")]
    public async Task<ActionResult> SendCommand(string deviceId, [FromBody] DeviceCommand command)
    {
        try
        {
            command.DeviceId = deviceId;
            
            // Handle Hue light commands
            var hueLight = await _hueService.GetLightAsync(deviceId);
            if (hueLight != null)
            {
                var success = await HandleHueCommand(deviceId, command);
                if (success)
                    return Ok(new { message = "Command executed successfully" });
            }
            
            return BadRequest("Failed to execute command");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command for device {DeviceId}", deviceId);
            return StatusCode(500, "Failed to execute command");
        }
    }

    private async Task<bool> HandleHueCommand(string deviceId, DeviceCommand command)
    {
        var state = new HueLightState();
        
        switch (command.Action.ToLower())
        {
            case "turn_on":
                state.On = true;
                break;
            case "turn_off":
                state.On = false;
                break;
            case "set_brightness":
                if (command.Parameters.TryGetValue("brightness", out var brightness))
                {
                    state.Brightness = Convert.ToInt32(brightness);
                    state.On = true;
                }
                break;
            case "set_color":
                if (command.Parameters.TryGetValue("hue", out var hue) &&
                    command.Parameters.TryGetValue("saturation", out var saturation))
                {
                    state.Hue = Convert.ToInt32(hue);
                    state.Saturation = Convert.ToInt32(saturation);
                    state.On = true;
                }
                break;
            case "set_color_temperature":
                if (command.Parameters.TryGetValue("colorTemperature", out var ct))
                {
                    state.ColorTemperature = Convert.ToInt32(ct);
                    state.On = true;
                }
                break;
            default:
                return false;
        }
        
        return await _hueService.SetLightStateAsync(deviceId, state);
    }
} 