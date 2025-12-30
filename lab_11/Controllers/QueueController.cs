using ASPA0011_1.Models;
using ASPA0011_1.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASPA0011_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly IChannelService _channelService;
        private readonly ILogger<QueueController> _logger;

        public QueueController(IChannelService channelService, ILogger<QueueController> logger)
        {
            _channelService = channelService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult ProcessQueueOperation([FromBody] QueueOperationRequest request)
        {
            _logger.LogTrace("POST /api/queue requested with operation: {Operation}", request.Operation);

            var result = _channelService.ProcessQueueOperation(request);

            if (!result.Success)
            {
                _logger.LogWarning("Queue operation failed: {Error}", result.Error);
                return NotFound(result);
            }

            _logger.LogDebug("Queue operation completed successfully: {Operation}", request.Operation);
            return Ok(result);
        }
    }
}