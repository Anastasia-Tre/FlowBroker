using System.Collections.Concurrent;
using System.Text;
using FlowBroker.Core.FlowPackets;

namespace FlowBroker.Client.TaskManager;

public interface ITaskManager : IDisposable
{
    Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge,
        CancellationToken cancellationToken);

    void OnPayloadOkResult(Guid payloadId);
    void OnPayloadErrorResult(Guid payloadId, FlowPacket error);
    void OnPayloadSendSuccess(Guid payloadId);
    void OnPayloadSendFailed(Guid payloadId);
}

internal class TaskManager : ITaskManager
{
    private readonly ConcurrentDictionary<Guid, SendPayloadTaskCompletionSource>
        _tasks;

    private bool _disposed;

    public TaskManager()
    {
        _tasks =
            new ConcurrentDictionary<Guid, SendPayloadTaskCompletionSource>();
        RunTaskCancelledCheckProcess();
    }

    public Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<SendAsyncResult>();

        var data = new SendPayloadTaskCompletionSource
        {
            CompleteOnAcknowledge = completeOnAcknowledge,
            TaskCompletionSource = tcs,
            CancellationToken = cancellationToken
        };

        _tasks[id] = data;

        return tcs.Task;
    }


    public void OnPayloadOkResult(Guid payloadId)
    {
        if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
            taskCompletionSource.OnOk();
    }

    public void OnPayloadErrorResult(Guid payloadId, FlowPacket error)
    {
        if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
            taskCompletionSource.OnError(
                Encoding.UTF8.GetString(error.Data.ToArray()));
    }

    public void OnPayloadSendSuccess(Guid payloadId)
    {
        if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
            taskCompletionSource.OnSendSuccess();
    }

    public void OnPayloadSendFailed(Guid payloadId)
    {
        if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
            taskCompletionSource.OnSendError();
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void RunTaskCancelledCheckProcess()
    {
        Task.Factory.StartNew(async () =>
        {
            while (!_disposed)
            {
                await Task.Delay(1000);
                DisposeCancelledTasks();
            }
        }, TaskCreationOptions.LongRunning);
    }

    private void DisposeCancelledTasks()
    {
        foreach (var (_, source) in _tasks)
            if (source.CancellationToken.IsCancellationRequested)
                source.OnError("Task was cancelled");
    }
}
