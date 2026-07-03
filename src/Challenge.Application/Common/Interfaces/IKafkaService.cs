using System.Threading;
using System.Threading.Tasks;

namespace Challenge.Application.Common.Interfaces;

public interface IKafkaService
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken) where T : class;
}
