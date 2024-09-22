namespace FlowBroker.Core.Utils.WaitThrottling;

public class DynamicWaitThrottling
{
    public DynamicWaitThrottling(int baseDelay = 1, int multiplier = 4,
        int maxDelay = 100)
    {
        BaseDelay = baseDelay;
        CurrentDelay = baseDelay;
        Multiplier = multiplier;
        MaxDelay = maxDelay;
    }

    public int MaxDelay { get; }
    public int Multiplier { get; }
    public int BaseDelay { get; }
    public int CurrentDelay { get; private set; }

    public Task WaitAsync(CancellationToken? cancellationToken = null)
    {
        return Task.Delay(CurrentDelay,
            cancellationToken ?? CancellationToken.None);
    }

    public Task WaitAndIncrease(CancellationToken? cancellationToken = null)
    {
        try
        {
            return WaitAsync(cancellationToken);
        }
        finally
        {
            Increase();
        }
    }

    public void Increase()
    {
        if (CurrentDelay * Multiplier > MaxDelay)
        {
            // do nothing
        }
        else
        {
            CurrentDelay *= Multiplier;
        }
    }

    public void Reset()
    {
        CurrentDelay = BaseDelay;
    }
}
