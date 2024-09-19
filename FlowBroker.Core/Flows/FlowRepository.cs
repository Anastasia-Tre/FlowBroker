using FlowBroker.Core.Utils.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.Flows;

public interface IFlowRepository : IDataRepository<string, IFlow>
{
    void Add(string name, string path);
}

public class FlowRepository : DataRepository<string, IFlow>, IFlowRepository
{
    private readonly List<IFlow> _queues;
    private readonly IServiceProvider _serviceProvider;

    public FlowRepository(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _queues = new List<IFlow>();
    }

    public override IEnumerable<IFlow> GetAll()
    {
        return _queues;
    }

    public void Add(string name, string path)
    {
        var queue = SetupQueue(name, path);
        _queues.Add(queue);
    }

    public override bool TryGet(string name, out IFlow flow)
    {
        flow = _queues.FirstOrDefault(q => q.Name == name);
        return flow != null;
    }

    public override bool Remove(string name)
    {
        var queueToRemove = _queues.FirstOrDefault(q => q.Name == name);

        if (queueToRemove == null)
            return false;

        return _queues.Remove(queueToRemove);
    }

    private IFlow SetupQueue(string name, string path)
    {
        var queue = _serviceProvider.GetRequiredService<IFlow>();
        queue.Setup(name, path);
        queue.StartProcessingFlowPackets();
        return queue;
    }
}

