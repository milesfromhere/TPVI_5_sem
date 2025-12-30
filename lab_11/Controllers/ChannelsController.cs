using ASPA0011_1.Models;
using ASPA0011_1.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASPA0011_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChannelsController : ControllerBase
    {
        private readonly IChannelService _channelService;
        private readonly ILogger<ChannelsController> _logger;

        public ChannelsController(IChannelService channelService, ILogger<ChannelsController> logger)
        {
            _channelService = channelService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetAllChannels()
        {
            _logger.LogTrace("GET /api/channels requested");

            var channels = _channelService.GetAllChannels();

            if (channels.Count == 0)
            {
                _logger.LogDebug("No channels found, returning 204");
                return NoContent();
            }

            _logger.LogDebug("Returning {ChannelCount} channels", channels.Count);
            return Ok(channels);
        }

        [HttpGet("{guid}")]
        public IActionResult GetChannel(Guid guid)
        {
            _logger.LogTrace("GET /api/channels/{Guid} requested", guid);

            var channel = _channelService.GetChannel(guid);

            if (channel == null)
            {
                _logger.LogError("Channel not found: {Guid}", guid);
                return NotFound();
            }

            _logger.LogDebug("Returning channel: {Guid}", guid);
            return Ok(channel);
        }

        [HttpPost]
        public IActionResult CreateChannel([FromBody] ChannelCreateRequest request)
        {
            _logger.LogTrace("POST /api/channels requested with data: {@Request}", request);

            // Валидируем статус, если он передан: допускаем только значения из enum
            if (request.Status != null &&
                request.Status != ChannelStatus.Active &&
                request.Status != ChannelStatus.Closed &&
                request.Status != ChannelStatus.Paused)
            {
                _logger.LogWarning("Invalid channel status value: {Status}", request.Status);
                return BadRequest("Invalid channel status value");
            }

            var channel = _channelService.CreateChannel(request);

            _logger.LogDebug("Channel created: {Guid}", channel.Guid);
            if(request.Status != ChannelStatus.Closed)
            {
                return StatusCode(201, channel);
            }
            else return StatusCode(204, channel);
        }

        // Для массового обновления
        [HttpPut]
        public IActionResult UpdateChannels([FromBody] ChannelUpdateRequest request)
        {
            if (request.Guid.HasValue)
            {
                // Обновление одного канала
                _logger.LogTrace("PUT /api/channels requested for single channel: {Guid}", request.Guid.Value);

                var channels = _channelService.UpdateChannel(request);

                _logger.LogDebug("Channel {ChannelGuid} updated to status: {Status}",
                    request.Guid.Value, request.Status);
                return Ok(channels);
            }
            else
            {
                // Обновление всех каналов
                _logger.LogTrace("PUT /api/channels requested for all channels with status: {Status}",
                    request.Status);

                var channels = _channelService.UpdateAllChannels(request.Status);

                _logger.LogDebug("All channels updated to status: {Status}", request.Status);
                return Ok(channels);
            }
        }
        [HttpDelete]
        public IActionResult DeleteChannel([FromBody] ChannelDeleteRequest request)
        {
            _logger.LogTrace("DELETE /api/channels requested ");

            var channels = _channelService.DeleteChannel(request);

            _logger.LogDebug("Channel deletion processed for ");
            return Ok(channels);
        }
    }
}