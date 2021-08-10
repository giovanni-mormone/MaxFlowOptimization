using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    public record Row(double[] Coeffs, double ConstraintValue, char ConstraintType);
}
