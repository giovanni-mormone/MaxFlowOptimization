using MaxFlowOptimizeDemo.jsonStructures;
using MaxFlowOptimizeDemo.jsonStructures.graphComponents;
using MaxFlowOptimizeDemo.result;
using System;
using System.Collections.Generic;
using System.Linq;
using MaxFlowOptimizeDemo.Utils;

namespace MaxFlowOptimizeDemo
{

    /// <summary>
    /// A class used to model a flow problem, using a <see cref="JsonProblem"/> as a source to create and model a flow
    /// problem.
    /// This alternative implementation sets sources and sink that don't have some commodity to 0
    /// </summary>
    public class FlowProblemFormulation : IFlowProblem
    {
        //private variables used by the the flowproblem modeler
        private static readonly double INFINITY = WrapperCoinMP.WrapperCoin.GetInfinity();
        private static readonly double PENALITY = 1000;
        //////////// variable modeling the graph
        private HashSet<string> nodes;
        private HashSet<CommoditySourceSink> sources;
        private HashSet<CommoditySourceSink> sinks;
        private HashSet<Commodity> commodities;
        private HashSet<Edge> edges;
        ////////////////////
        /////////variables used to store the constraints and the objective function
        private List<double> objectiveCoeffs;
        private HashSet<Row> rows;
        ////////
        
        //value given in input and used in the first formulation to make it possible to duplicate commodities
        //going out of a broker
        private readonly int nMaxMultiplier;
        //hashset holding all the penality edges
        private HashSet<Edge> penality = new();
        //dictionary used in the first formulation, old the original commodities as keys associate to the duplicated
        //commodities source/sink
        Dictionary<string, List<string>> commodityGroups = new();

        /// <summary>
        /// Constructor of this <see cref="IFlowProblem"/> implementation:
        /// it can get the nMax used by the first formulation if needed;
        /// if not needed it could be omitted.
        /// </summary>
        /// <param name="nMax"> The nMax needed in the first formulation; optional parameter</param>
        public FlowProblemFormulation(int nMax = 0)
        {
            nMaxMultiplier = nMax;
        }

        public void InizializeProblem(JsonProblem loadedProblem, Dictionary<string, List<string>> commodityGroups)
        {
            this.commodityGroups = commodityGroups;
            InitializeNodesAndEdges(loadedProblem);
            InitializeObjectiveCoeffsFirstFormulation();
            InitializeRows();
            InitializeCapacityRows();
        }

        public void InizializeProblemAlternativeFormulation(JsonProblem loadedProblem)
        {
            InitializeNodesAndEdges(loadedProblem);
            InitializeObjectiveCoeffsSecondFormulation();
            InitializeRowsSecondFormulation();
        }
        public void CreateLagrangian(JsonProblem loadedProblem, Dictionary<string, List<string>> commodityGroups, int lambda)
        {
            this.commodityGroups = commodityGroups;
            InitializeNodesAndEdges(loadedProblem);
            InitializeObjectiveCoeffsLagrangian(lambda);
            InitializeRows();
        }

        public Result CreateResult(double result, List<double> optimizedValues) => new Result(result, commodities.SelectMany(commodity => edges.Select((edge, edgeIndex) =>
                new OptimizedEdge(edge.Source, edge.Destination, commodity.CommodityName, 
                    optimizedValues.ElementAt(ComputeIndexInEdgeResult(edgeIndex, commodity.CommodityNumber, edges.Count))))).ToList());
        
        public HashSet<Row> GetRows() => rows.ToHashSet();
        
        public double[] GetObjectiveCoeffs() => objectiveCoeffs.ToArray();

        public void AddPenality(Edge edge) => penality.Add(edge);
        
        //questo metodo prende in input i coefficienti associati a determinati archi;
        //usa il numero di commodity passato per decidere in che posizione effettiva mettere i coefficienti passati,
        //mentre il totale gli serve per capire quanti sono gli spazi che vanno settati a 0.
        private List<double> CreateRow(List<double> coeffs, int commodity, int totalCommodities)
        {
            
            var zeroRow = RepeatedZeroList(coeffs.Count);
            //questa finalZero serve a mettere le Xi a zero dopo tutte le variabili gestite;
            //notare che questa cosa solo per la prima formulazione dato che nella seconda i commodity groups
            //non sono settati e quindi sarà lunga 0
            var finalZero = RepeatedZeroList(coeffs.Count * commodityGroups.Keys.Count);
            
            //il metodo è tail recursive e inizializza la riga con tutti 0 tranne per la zona
            //con la commodity interessata
            List<double> _createRow(IEnumerable<double> origin, int step) => step switch
            {
                _ when step == totalCommodities => origin.Concat(finalZero).ToList(),
                _ when step == commodity => _createRow(origin.Concat(coeffs), step + 1),
                _ => _createRow(origin.Concat(zeroRow), step + 1)
            };
            return _createRow(Enumerable.Empty<double>(), 0);
        }

        //metodo che semplicemente legge e salva i dati del grafo passato in input in strutture interne
        private void InitializeNodesAndEdges(JsonProblem loaded)
        {
            //il commodity number è ottenuto semplicemente in base all'ordine delle commodity nel problema passato
            //e serve per capire a che posizione dei vari array di coefficienti e righe è associato
            commodities = loaded.Commodities.Select((name, id) => (name, id)).Select(x => new Commodity(x.name, x.id)).ToHashSet();
            sources = loaded.CommoditiesSources.ToHashSet();
            sinks = loaded.CommoditiesSinks.ToHashSet();
            nodes = loaded.Nodes.ToHashSet();
            edges = RangeList(loaded.Edges.Count).Select(x => loaded.Edges.ElementAt(x)).ToHashSet();
        }

        //metodo che inizializza la funzione obiettivo della prima formulazione
        private void InitializeObjectiveCoeffsFirstFormulation()
        {
            //prima creo una lista con tanti zero quante sono le commodity finte + quelle originali:
            //le originali modellano le Xi.
            List<double> startingList = RepeatedZeroList(edges.Count * (commodities.Count + commodityGroups.Keys.Count)).ToList();

            objectiveCoeffs = CreateObjectiveWithPenality(startingList);
        }

        //La seconda formulazione è diversa dalla prima perchè setta tutte le variabili associate alle commodity == 1 ed i costi
        // delle var penalità == Penality, mentre le Xi che utilizza sono == 0; non è sufficiente quindi creare una lista di 0
        //comprendente anche le Xi ma le aggiungo dopo aver creato il primo pezzo con i costi.
        private void InitializeObjectiveCoeffsSecondFormulation()
        {
            List<double> startingList = Enumerable.Repeat(1.0, edges.Count * commodities.Count).ToList();
            objectiveCoeffs = CreateObjectiveWithPenality(startingList).Concat(RepeatedZeroList(edges.Count * commodities.Count)).ToList();
        }
        //La lagrangiana associata alla prima formulazione crea la prima parte della funzione obiettivo in maniera uguale alla prima,
        // con la differenza di inizializzare le Xi associandogli un costo
        private void InitializeObjectiveCoeffsLagrangian(int lambda)
        {
            List<double> startingList = RepeatedZeroList(edges.Count * (commodities.Count)).ToList();
            //la lista XiLagPenality è associata alle variabili Xi, e viene creata utilizzando il Lambda; in questa versione il lambda è fisso e costante

/*            rows = rows.Concat(edges.Select((edge, column) =>
               new Row(RangeList(commodities.Count + commodityGroups.Keys.Count).SelectMany(commodityNumber => RepeatedZeroList(edges.Count)
                   .Select((value, col) => col == column && commodityNumber >= commodities.Count ? FindAkCommodity(findChildCommodityNumber(commodityNumber)) : value).ToList()).ToArray(), edge.Weight == -1 ? INFINITY : edge.Weight, 'L')))
               .ToHashSet();*/

            List<double> XiLagPenalty = commodityGroups.Keys.Select((_,index) => commodities.Count + index).SelectMany(trueCommodity =>
            {
                return edges.Select(_ => FindAkCommodity(findChildCommodityNumber(trueCommodity)) * lambda).ToList();
            }).ToList();

            //List<double> XiLagPenalty = RepeatedZeroList(edges.Count * commodityGroups.Keys.Count).Select(x => x + 100).ToList();
            objectiveCoeffs = CreateObjectiveWithPenality(startingList).Concat(XiLagPenalty).ToList();

        }
        //questo metodo modifica la lista passati in input e gli mette un valore == a PENALITY nelle posizioni associate agli archi con penalità
        private List<double> CreateObjectiveWithPenality(List<double> StartingCoeffs)
        {
            //la variabile x è ottenuta cercando tutti gli indici associati agli archi con penalità, che poi saranno usati sotto 
            //per modificare la lista di input.
            var x = edges.Select((edge, index) => (penality.FirstOrDefault(penal => penal.Equals(edge)), index))
                .Where(x => x.Item1 != default).SelectMany(couple =>
                 commodities.Select(commo => couple.index + commo.CommodityNumber * edges.Count).ToList()).ToList();
            return StartingCoeffs.Select((val, index) => x.Contains(index) ? PENALITY : val).ToList();
        }


        //questo metodo inizializza i vincoli per la prima formulazione; è utilizzata anche dalla lagrangiana della prima formulazione,
        //dato che i vincoli tra le due sono gli stessi con la differenza che la lagrangiana non ha i vincoli di capacità che vengono
        //aggiunti alla prima formula
        private void InitializeRows()
        {
            ///// aggiunta dei vincoli per sorgenti e destinazioni
            rows = SourceSinkInitialize(true);
            rows = rows.Concat(SourceSinkInitialize(false)).ToHashSet();
            ////

            //aggiunta del vincolo per la gestione dei flussi in un nodo; si fa in modo che la somma di quello che entra e quello che esce
            //sia uguale a 0
            rows = rows.Concat(nodes.SelectMany(node =>
            {
                var nodeEdges = edges.Select((edge, column) => (edge, column))
                    .Where(combo => combo.edge.Source == node || combo.edge.Destination == node).ToList();

                //prima creo una lista di zero lunga quanto gli archi, faccio una select per accoppiare valore ed indice nella lista
                var rowCoeffs = RepeatedZeroList(edges.Count).Select((value,index) =>
                {
                    //poi cerco nella lista di archi associati al nodo se ne è presente uno con colonna = all'indice trattato
                    var cop = nodeEdges.FirstOrDefault(nod => nod.column == index);
                    //se non è presente quindi == defalt, ritorno il valore originale della lista di zeri creata
                    //altrimenti ritorno -1 se sono una sorgente e quindi il valore esce o 1 se sono destinazione e quindi entra.
                    return cop == default ? value : cop.edge.Source == node ? -1 : 1;
                }).ToList();

                //poi torno le righe creandone una per commodity
                return RangeList(commodities.Count).Select(x => new Row(CreateRow(rowCoeffs, x, commodities.Count).ToArray(), 0, 'E'));
            })).ToHashSet();

            //ultimo passo si aggiungono i vincoli per gestire la condivisione del flusso su un arco
            //settando nMax come valore da sottrarre alla variabile Xi associata agli archi che condividono una commodity
            rows = rows.Concat(commodityGroups.Select(pair => pair.Value).ToList().SelectMany(commodities =>
            {
                //questo vincolo si applica a tutti gli archi tranne quelli che vanno verso le dummyDestinations;
                //per ogni arco lo mappo associandogli la su colonna e costruisco la riga
                return edges.Where(edge => sinks.All(sink => sink.Name != edge.Destination)).Select((edge, column) =>
                    new Row(constructMultiCommodityRow(commodities, column), 0, 'L')).ToHashSet();
            })).ToHashSet();
        }

        //metodo che inizializza i vincoli per le capacità delle righe nella prima formulazione
        private void InitializeCapacityRows()
        {
            //per ogni arco prima gli si associa la sua colonna poi si crea una riga di zeri lunga quanto tutte le variabili del problema
            // in ultimo si modificano le colonne associate alle Xi per gestire il vincolo di capacità (si assume che chi legge conosce di che 
            //vincoli e formulazione si sta parlando)
            rows = rows.Concat(edges.Select((edge, column) =>
               new Row(RangeList(commodities.Count + commodityGroups.Keys.Count).SelectMany(commodityNumber => RepeatedZeroList(edges.Count)
                   .Select((value, col) => col == column && commodityNumber >= commodities.Count ? FindAkCommodity(findChildCommodityNumber(commodityNumber)) : value).ToList()).ToArray(), edge.Weight == -1 ? INFINITY : edge.Weight, 'L')))
               .ToHashSet();
        }

        //metodo che trova il numero di una commodity figlia della commodity presa in input;
        // viene utilizzato quando si scorrono delle liste lunghe quanto la somma di commodity figlie ed originali(quindi nella prima formulazione)
        private int findChildCommodityNumber(int num)
        {
            //il numero in input viene trasformato in un indice compatibile con le chiavi del del dictionary di commo originali
            num = num - commodities.Count();
            //viene poi usato per trovare la chiave del dictionary
            var key = commodityGroups.Keys.ToList().ElementAt(num);
            // poi ritorno il numero di commodity della prima commodity associata alla chiave, dato che non ne cerco una precisa.
            return commodities.First(x => x.CommodityName == commodityGroups[key].First()).CommodityNumber;
        }

        //metodo usato per costruire i coefficienti usati nel vincolo del flusso su un arco, quello in cui va gestito il flusso su archi veri
        // e Xi in base alle commodity
        //viene gestito per pezzi, cioè associando 1 quando si tratta di una commodity finta associata alla colonna passata in input; la commodity
        // deve essere contenuta nella lista di commodity passate in input; se stiamo lavorando su una colonna che è uguale a quella di input e
        // siamo oltre le commodity finte ma stiamo lavorando su una commodity di interesse si assegna alla colonna il valore -nMaxMultiplier altrimenti 0
        private double[] constructMultiCommodityRow(List<string> commo, int column) => 
            RangeList(commodities.Count + commodityGroups.Keys.Count).SelectMany(commodityNumber => RepeatedZeroList(edges.Count).
                Select((value, col) => col == column && commodities.Where(x => commo.Contains(x.CommodityName)).
                    Any(x => x.CommodityNumber == commodityNumber) ? 1.0 : col == column && commodityNumber >= commodities.Count && commodities.Where(x => commo.Contains(x.CommodityName))
                        .Any(x => x.CommodityNumber == findChildCommodityNumber(commodityNumber)) ? -nMaxMultiplier : 0).ToList()).ToArray();
            
       
        /////////questa zona la commento meglio quando finisce la formulazione di boschetti, sono metodi associati a quella.
        private HashSet<Row> EdgeConstraints(Edge edge)
        {

            return commodities.Select(commodity =>
            {
                var predecessors = edges.Select((edge, index) => (edge, index)).Where(x => x.edge.Destination == edge.Source).ToList();

                predecessors = predecessors.Where(x => (sources.All(source => source.Name != x.edge.Source) && sinks.All(sink => sink.Name != x.edge.Source)) ||
                    sources.Any(source => source.Name == x.edge.Source && source.Commodity == commodity.CommodityName)
                    ).ToList();
                var edgerow = RepeatedZeroList(edges.Count).Select((x, col) => predecessors.Any(x => x.index == col) ? -1 : x).ToList();

                if (sinks.Any(x => x.Name == edge.Source && x.Commodity == commodity.CommodityName))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), -1, 'L');
                }
                else if (sinks.Any(x => x.Name == edge.Destination && x.Commodity != commodity.CommodityName))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), 0, 'L');
                }
                else if (sources.Any(source => source.Name == edge.Destination && source.Commodity == commodity.CommodityName))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), 0, 'L');
                }
                else if (sources.All(x => x.Name != edge.Source) && sinks.All(x => x.Name != edge.Source))
                {
                    return new Row(CreateRow(edgerow, commodity.CommodityNumber, commodities.Count).ToArray(), -1, 'L');
                }
                else
                {
                    return new Row(CreateRow(RepeatedZeroList(edges.Count), commodity.CommodityNumber, commodities.Count).ToArray(), 0, 'L');
                }
            }).ToHashSet();
        }

        private HashSet<Row> SetInequalitiesConstraints()
        {

            var edgesAndColumns = edges.Select((edge, column) => (edge, column)).ToList();
            HashSet<Row> result = new HashSet<Row>();

            result = result.Concat(edgesAndColumns.SelectMany(edge =>
            {
                return EdgeConstraints(edge.edge);
            })).ToHashSet();

            return result;
        }

        private void InitializeRowsSecondFormulation()
        {
            rows = SetInequalitiesConstraints();
            rows = rows.Concat(edges.Select((edge, column) =>
                new Row(RangeList(commodities.Count).SelectMany(commodityNumber => RepeatedZeroList(edges.Count)
                    .Select((value, col) => col == column ? FindAkCommodity(commodityNumber) : value).ToList()).Concat(RepeatedZeroList(edges.Count * commodities.Count)).ToArray(), edge.Weight == -1 ? INFINITY : edge.Weight, 'L')))
                .ToHashSet();
        }

        ////////////////////

        //metodo che trova il peso di una commodity a partire dal suo numero
        private double FindAkCommodity(int commodityNumber) => sources.First(x => x.Commodity == commodities.First(xx => xx.CommodityNumber == commodityNumber).CommodityName).Capacity;

        //Method used to compute, in a tail recursive way, the row constraints of sources and sinks;
        //It uses the loaded problem, a boolean to decide if it is source or sink,
        private HashSet<Row> SourceSinkInitialize(bool IsSource)
        {
            //first i set the right filter functions for the tail method.
            var findSourceSink = IsSource ? FindSources : FindSinks;
            var findCommoditySourceSink = IsSource ? FindCommoditySource : FindCommoditySink;

            //then recursively compute the set of constraint for all the sources or sinks of the problem;
            //First case is the base case => when the input list has only one element left, it first computes the constraint for the CommoditySourceSink
            //then it computes all the constraints for the not contained commodities.
            //Second case is when a source/sink has multiple commodities(the input list is ordered when the function is called first time): it computes the
            //constraint for the given source/sink then it call itself recursively.
            //third case is when the element to compute is the last for the given source/sink: it computes the his constraint, then it computes the constraints for
            //all the commodities that are not generated/required by him and then it calls itself recursively.
            HashSet<Row> _SourceSinkInitialize(List<CommoditySourceSink> sources, HashSet<Row> rowRecursive) => sources switch
            {
                (CommoditySourceSink source, _) when sources.Count == 1 =>
                         rowRecursive.Append(ContainedCommodityConstraint(Metodino(source, findSourceSink)))
                         .Concat(NotContainedCommoditiesConstraints(Metodino(source, findSourceSink), findCommoditySourceSink, IsSource)).ToHashSet(),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name == tail.First().Name =>
                        _SourceSinkInitialize(tail, rowRecursive.Append(ContainedCommodityConstraint(Metodino(source, findSourceSink))).ToHashSet()),
                (CommoditySourceSink source, List<CommoditySourceSink> tail) when source.Name != tail.First().Name =>
                        _SourceSinkInitialize(tail, rowRecursive.Append(ContainedCommodityConstraint(Metodino(source, findSourceSink)))
                        .Concat(NotContainedCommoditiesConstraints(Metodino(source, findSourceSink), findCommoditySourceSink, IsSource)).ToHashSet()),
                _ => rowRecursive,
            };
            return _SourceSinkInitialize(IsSource ? sources.OrderBy(x => x.Name).ToList() : sinks.OrderBy(x => x.Name).ToList(), new());
        }

        //method used to create the SourceSinkCoeffs for the given CommoditySourceSink.
        private SourceSinkCoeffs Metodino(CommoditySourceSink source, Func<Edge, string, bool> findSourceSink)
        {
            var sourceEdges = edges.Select((edge, column) => (edge, column)).Where(edge => findSourceSink(edge.edge, source.Name)).ToList();
            var rowCoeffs = RepeatedZeroList(edges.Count);
            sourceEdges.ForEach(edge => rowCoeffs[edge.column] = 1);
            return new SourceSinkCoeffs(source, rowCoeffs);
        }
        //Method that create the constraint for a given source or sink.
        private Row ContainedCommodityConstraint(SourceSinkCoeffs values)
        {
            var contained = commodities.First(commo => commo.CommodityName == values.source.Commodity).CommodityNumber;
            return new Row(CreateRow(values.rowCoeffs, contained, commodities.Count).ToArray(), 1.0, 'E');
        }

        //metodo per le commodity non generate da una sorgente o ricevute da un pozzo
        private HashSet<Row> NotContainedCommoditiesConstraints(SourceSinkCoeffs values, Func<HashSet<CommoditySourceSink>, CommoditySourceSink, List<string>> FindCommodities, bool isSource)
        {
            var myCommodities = FindCommodities(isSource ? sources : sinks, values.source);
            var contained = commodities.Where(commo => !myCommodities.ToList().Contains(commo.CommodityName)).Select(x => x.CommodityNumber).ToList();
            return contained.Select(x => new Row(CreateRow(values.rowCoeffs, x, commodities.Count).ToArray(), 0, 'E')).ToHashSet();
        }

        //record used to store the data of a given source/sink.
        private record SourceSinkCoeffs(CommoditySourceSink source, List<double> rowCoeffs);
        //Static private func section;
        //This methods are used to initialize the source and sinks constraints.
        private static readonly Func<HashSet<CommoditySourceSink>, CommoditySourceSink, List<string>> FindCommoditySource = (sources, source) => sources.Where(x => x.Name == source.Name).Select(xx => xx.Commodity).ToList();
        private static readonly Func<HashSet<CommoditySourceSink>, CommoditySourceSink, List<string>> FindCommoditySink = (sinks, sink) => sinks.Where(x => x.Name == sink.Name).Select(xx => xx.Commodity).ToList();
        private static readonly Func<Edge, string, bool> FindSources = (edge, name) => edge.Source == name;
        private static readonly Func<Edge, string, bool> FindSinks = (edge, name) => edge.Destination == name;
        private static readonly Func<int, int, int, int> ComputeIndexInEdgeResult = (edgeIndex, commodity, totalEdges) => edgeIndex + totalEdges * commodity;
        private static readonly Func<int, List<double>> RepeatedZeroList = length => Enumerable.Repeat(0.0, length).ToList();
        private static readonly Func<int, HashSet<int>> RangeList = length => Enumerable.Range(0, length).ToHashSet();
    }
}