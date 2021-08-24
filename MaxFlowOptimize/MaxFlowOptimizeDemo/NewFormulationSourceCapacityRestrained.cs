using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxFlowOptimizeDemo
{
    class NewFormulationSourceCapacityRestrained : NewFormulationFlowProblem
    {
        public NewFormulationSourceCapacityRestrained(int nMax) : base(nMax) { }

        protected override void InitializeRows(JsonProblem loadedProblem)
        {
            base.InitializeRows(loadedProblem);

            rows = rows.Concat(sources.SelectMany(source =>
                edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == source.Name)
                    .Select(edge =>
                        new Row(RangeList(loadedProblem.Commodities.Count).SelectMany(_ => RepeatedZeroList(loadedProblem.Edges.Count)
                            .Select((value, column) => column == edge.column ? 1 : value).ToList()).ToArray(), edge.edge.Weigth == -1 ? INFINITY : edge.edge.Weigth, 'L')
                )
            )).ToHashSet();

        }
    }
}
