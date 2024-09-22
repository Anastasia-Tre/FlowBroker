using FlowBroker.Core.Utils.Pooling;

namespace FlowBroker.Core.Payload;

public class AsyncPayloadTicket : IPooledObject
{
    private bool _isStatusSet;

    public Guid PayloadId { get; private set; }

    public bool IsEmpty => OnStatusChanged == null;

    public Guid PoolId { get; set; }

    public event Action<Guid, bool> OnStatusChanged;

    public void Setup(Guid payloadId)
    {
        PayloadId = payloadId;
        ClearStatusListener();
    }

    private void ClearStatusListener()
    {
        _isStatusSet = false;
        OnStatusChanged = null;
    }

    public void SetStatus(bool success)
    {
        lock (this)
        {
            if (_isStatusSet)
                throw new Exception(
                    $"{nameof(SerializedPayload)} status is already set");

            _isStatusSet = true;

            OnStatusChanged?.Invoke(PayloadId, success);
        }
    }
}
