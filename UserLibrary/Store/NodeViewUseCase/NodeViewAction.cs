using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Store.NodeViewUseCase
{
    public class NodeViewAction
    {
        public NodeViewState historyState { get; set; }
    }

    public class LoadHistoryAction
    {
        public NodeViewState historyState { get; set; }
    }

    public class GetProfitAction
    {
        public string owner { get; set; }
    }

    public class ReturnToMainAction
    {
        
    }
}
