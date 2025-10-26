using DotnetVoyager.BLL.Models;
using System.Threading.Channels;

namespace DotnetVoyager.BLL.Workers;

public interface IDecompilationTaskQueue
{
    ValueTask EnqueueAsync(AnalysisTask task);
    ValueTask<AnalysisTask> DequeueAsync(CancellationToken cancellationToken);
}

public class DecompilationTaskQueue : IDecompilationTaskQueue
{
    private readonly Channel<AnalysisTask> _queue;

    public DecompilationTaskQueue()
    {
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
