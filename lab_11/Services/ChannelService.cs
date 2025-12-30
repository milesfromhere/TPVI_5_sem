using System;
using System.Threading.Channels;
using ASPA0011_1.Models;
using Microsoft.Extensions.Options;

namespace ASPA0011_1.Services
{
    public interface IChannelService
    {
        List<ChannelInfo> GetAllChannels();
        ChannelInfo? GetChannel(Guid guid);
        ChannelInfo CreateChannel(ChannelCreateRequest request);
        List<ChannelInfo> UpdateAllChannels(ChannelStatus status);
        List<ChannelInfo> UpdateChannel(ChannelUpdateRequest request);
        List<ChannelInfo> DeleteChannel(ChannelDeleteRequest request);
        QueueOperationResponse ProcessQueueOperation(QueueOperationRequest request);
    }

    public class ChannelService : IChannelService
    {
        private readonly Dictionary<Guid, ChannelWrapper> _channels;
        private readonly ILogger<ChannelService> _logger;
        private readonly int _waitEnqueue;

        public ChannelService(ILogger<ChannelService> logger, IConfiguration configuration)
        {
            _channels = new Dictionary<Guid, ChannelWrapper>();
            _logger = logger;
            _waitEnqueue = configuration.GetValue<int>("WaitEnqueue", 30);
        }

        public List<ChannelInfo> GetAllChannels()
        {
            _logger.LogDebug("Getting all channels. Total channels: {ChannelCount}", _channels.Count);
            return _channels.Values.Select(c => c.Info).ToList();
        }

        public ChannelInfo? GetChannel(Guid guid)
        {
            _logger.LogDebug("Getting channel with GUID: {ChannelGuid}", guid);
            return _channels.TryGetValue(guid, out var channel) ? channel.Info : null;
        }

        public ChannelInfo CreateChannel(ChannelCreateRequest request)
        {
            if(request.Status == null)
            {
                var channelWrapper = new ChannelWrapper(request.Name, request.Description);
                _channels[channelWrapper.Info.Guid] = channelWrapper;
                _logger.LogInformation("Created new channel: {ChannelName} with GUID: {ChannelGuid}",
                    request.Name, channelWrapper.Info.Guid);
                return channelWrapper.Info;
            }
            else
            {
                var channelWrapper = new ChannelWrapper(request.Name, request.Description, request.Status);
                _channels[channelWrapper.Info.Guid] = channelWrapper;
                _logger.LogInformation("Created new channel: {ChannelName} with GUID: {ChannelGuid}",
                    request.Name, channelWrapper.Info.Guid);
                return channelWrapper.Info;
            }
        }

        public List<ChannelInfo> UpdateAllChannels(ChannelStatus status)
        {
            _logger.LogInformation("Updating all channels to status: {Status}", status);

            foreach (var channel in _channels.Values)
            {
                if (channel.Info.Status != status)
                {
                    channel.Info.Status = status;
                    _logger.LogDebug("Updated channel {ChannelGuid} to status {Status}",
                        channel.Info.Guid, status);
                }
            }

            return GetAllChannels();
        }

        public List<ChannelInfo> UpdateChannel(ChannelUpdateRequest request)
        {
            _logger.LogDebug("Updating channel with guid: {Guid} to status: {Status}", request.Guid, request.Status);

            if (_channels.TryGetValue((Guid)request.Guid, out var channel))
            {
                if (channel.Info.Status != request.Status)
                {
                    channel.Info.Status = request.Status;

                    _logger.LogDebug("Updated channel {ChannelGuid} to status {Status}",
                        channel.Info.Guid, request.Status);
                }
            }

            return GetAllChannels();
        }

        public List<ChannelInfo> DeleteChannel(ChannelDeleteRequest request)
        {
            if (request.status != null)
            {
                foreach(Guid GuidChannel in _channels.Keys)
                {
                    ChannelWrapper channel = _channels[GuidChannel];
                    if (channel.Info.Status == request.status) _channels.Remove(GuidChannel);
                    _logger.LogDebug("Deleting channel: {ChannelGuid}", GuidChannel);
                }
            }
            else
            {
                _logger.LogInformation("Deleting all channels");
                foreach (Guid GuidChannel in _channels.Keys)
                {
                    _channels.Remove(GuidChannel);
                    _logger.LogDebug("Deleting channel: {ChannelGuid}", GuidChannel);
                }
            }

            return GetAllChannels();
        }

        public QueueOperationResponse ProcessQueueOperation(QueueOperationRequest request)
        {
            _logger.LogDebug("Processing queue operation: {Operation} for channel: {ChannelGuid}",
                request.Operation, request.ChannelGuid);

            if (!_channels.TryGetValue(request.ChannelGuid, out var channelWrapper))
            {
                _logger.LogError("Channel not found for queue operation: {ChannelGuid}", request.ChannelGuid);
                return new QueueOperationResponse { Success = false, Error = "Channel not found" };
            }

            try
            {
                return request.Operation switch
                {
                    QueueOperationType.Enqueue => EnqueueOperation(channelWrapper, request),
                    QueueOperationType.Dequeue => DequeueOperation(channelWrapper, request),
                    QueueOperationType.Peek => PeekOperation(channelWrapper, request),
                    _ => new QueueOperationResponse { Success = false, Error = "Unknown operation" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue operation {Operation} for channel {ChannelGuid}",
                    request.Operation, request.ChannelGuid);
                return new QueueOperationResponse { Success = false, Error = ex.Message };
            }
        }

        private QueueOperationResponse EnqueueOperation(ChannelWrapper channel, QueueOperationRequest request)
        {
            var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds ?? _waitEnqueue);

            if(channel.Info.Status == ChannelStatus.Closed)
            {
                _logger.LogWarning("Enqueue timeout for channel: {ChannelGuid}", request.ChannelGuid);
                return new QueueOperationResponse { Success = false, Error = "Enqueue timeout" };
            }
            var success = channel.Writer.TryWrite(request.Data);

            if (!success)
            {
                _logger.LogWarning("Enqueue timeout for channel: {ChannelGuid}", request.ChannelGuid);
                return new QueueOperationResponse { Success = false, Error = "Enqueue timeout" };
            }

            _logger.LogDebug("Successfully enqueued data to channel: {ChannelGuid}", request.ChannelGuid);
            return new QueueOperationResponse { Success = true, Data = "Enqueued successfully" };
        }

        private QueueOperationResponse DequeueOperation(ChannelWrapper channel, QueueOperationRequest request)
        {
            var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds ?? _waitEnqueue);

            if (channel.Reader.TryRead(out var item))
            {
                _logger.LogDebug("Successfully dequeued data from channel: {ChannelGuid}", request.ChannelGuid);
                return new QueueOperationResponse { Success = true, Data = item };
            }

            _logger.LogWarning("Dequeue timeout for channel: {ChannelGuid}", request.ChannelGuid);
            return new QueueOperationResponse { Success = false, Error = "Dequeue timeout" };
        }

        private QueueOperationResponse PeekOperation(ChannelWrapper channel, QueueOperationRequest request)
        {
            _logger.LogDebug("Peek operation for channel: {ChannelGuid}", request.ChannelGuid);
            return new QueueOperationResponse { Success = true, Data = channel.Info };
        }
    }

    public class ChannelWrapper
    {
        public Channel<object> Channel { get; }
        public ChannelInfo Info { get; }
        public ChannelReader<object> Reader => Channel.Reader;
        public ChannelWriter<object> Writer => Channel.Writer;

        public ChannelWrapper(string? name, string? description)
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<object>();
            Info = new ChannelInfo
            {
                Guid = Guid.NewGuid(),
                Name = name,
                Description = description,
                Status = ChannelStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }
        public ChannelWrapper(string? name, string? description, ChannelStatus? status)
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<object>();
            Info = new ChannelInfo
            {
                Guid = Guid.NewGuid(),
                Name = name,
                Description = description,
                Status = (ChannelStatus)status,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}