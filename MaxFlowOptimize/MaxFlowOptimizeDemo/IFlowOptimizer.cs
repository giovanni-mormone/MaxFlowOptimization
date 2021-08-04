using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    interface IFlowOptimizer
    {
        void ReadFromJSON(string path);
    }
}
