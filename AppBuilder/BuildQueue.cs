using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AppBuilder {
    public interface IBuildQueue {
        void Queue(string agentName, string host);
        Task<BuildModel> DequeueAsync(CancellationToken cancellationToken);
    }

    public class BuildQueue : IBuildQueue {
        private readonly ConcurrentQueue<BuildModel> _buildModels = new ConcurrentQueue<BuildModel>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void Queue(string agentName, string host) {
            _buildModels.Enqueue(new BuildModel {
                AgentName = agentName,
                Host = host
            });
            _signal.Release();
        }

        public async Task<BuildModel> DequeueAsync(CancellationToken cancellationToken) {
            await _signal.WaitAsync(cancellationToken);
            _buildModels.TryDequeue(out var model);
            return model;
        }
    }
}