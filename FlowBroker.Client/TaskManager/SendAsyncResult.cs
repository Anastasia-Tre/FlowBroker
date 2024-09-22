using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Client.TaskManager;

public class SendAsyncResult
{
    public bool IsSuccess { get; set; }

    public string InternalErrorCode { get; set; }
}
