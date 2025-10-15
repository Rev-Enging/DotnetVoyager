using DotnetVoyager.BLL.Models;
using System.Threading.Channels;

namespace DotnetVoyager.BLL.Workers;

public interface IBackgroundTaskQueue
{
    ValueTask EnqueueAsync(AnalysisTask task);
    ValueTask<AnalysisTask> DequeueAsync(CancellationToken cancellationToken);
}


public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<AnalysisTask> _queue;

    public BackgroundTaskQueue()
    {
        // A Channel is a thread-safe data structure perfect for producer-consumer scenarios.
        // Unbounded means it can grow as needed. For production, you might consider a Bounded channel
        // to prevent excessive memory usage if the consumer can't keep up.
        _queue = Channel.CreateUnbounded<AnalysisTask>();
    }

    public async ValueTask EnqueueAsync(AnalysisTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        await _queue.Writer.WriteAsync(task);
    }

    public async ValueTask<AnalysisTask> DequeueAsync(CancellationToken cancellationToken)
    {
        var task = await _queue.Reader.ReadAsync(cancellationToken);
        return task;
    }
}