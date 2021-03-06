﻿// The Software was produced by Battelle under Contract No. DE-AC05-76RL01830
// with the Department of Energy.  The U.S. Government is granted for itself and others 
// acting on its behalf a nonexclusive, paid-up, irrevocable worldwide license in this data 
// to reproduce, prepare derivative works, distribute copies to the public, perform 
// publicly and display publicly, and to permit others to do so.  The specific term of the 
// license can be identified by inquiry made to Battelle or DOE.  
// 
// NEITHER THE UNITED STATES NOR THE UNITED STATES DEPARTMENT OF ENERGY, 
// NOR ANY OF THEIR EMPLOYEES, MAKES ANY WARRANTY, EXPRESS OR IMPLIED,
// OR ASSUMES ANY LEGAL LIABILITY OR RESPONSIBILITY FOR THE ACCURACY, 
// COMPLETENESS OR USEFULNESS OF ANY DATA, APPARATUS, PRODUCT OR PROCESS
// DISCLOSED, OR REPRESENTS THAT ITS USE WOULD NOT INFRINGE PRIVATELY OWNED 
// RIGHTS.
namespace ImsInformed.Domain.DataAssociation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using ImsInformed.Domain.DirectInjection;

    using QuickGraph;
    using QuickGraph.Graphviz;

    /// <summary>
    /// The observation transition graph.
    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    internal class ObservationTransitionGraph<T> where T : Edge<ObservedPeak>
    {
        /// <summary>
        /// The base peak map.
        /// </summary>
        private IDictionary<VoltageGroup, IList<ObservedPeak>> basePeakMap;

        /// <summary>
        /// The sorted voltage group increasing voltage.
        /// </summary>
        private readonly IList<VoltageGroup> sortedVoltageGroupIncreasingVoltage;

        /// <summary>
        /// The sorted voltage group decreasing voltage.
        /// </summary>
        private readonly IList<VoltageGroup> sortedVoltageGroupDecreasingVoltage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservationTransitionGraph{T}"/> class.
        /// </summary>
        /// <param name="observations">
        /// </param>
        /// <param name="transitionFunction">
        /// </param>
        public ObservationTransitionGraph(IEnumerable<ObservedPeak> observations, Func<ObservedPeak, ObservedPeak, T> transitionFunction)
        {
            // Create the data structure as the input to the combinatorial algorithm
            observations = observations.ToArray();

            // Create the base peak map
            this.basePeakMap = new Dictionary<VoltageGroup, IList<ObservedPeak>>();
            foreach (ObservedPeak observation in observations)
            {
                VoltageGroup voltageGroup = observation.VoltageGroup;
                if (this.basePeakMap.ContainsKey(voltageGroup))
                {
                    this.basePeakMap[voltageGroup].Add(observation);
                }
                else
                {
                    this.basePeakMap.Add(voltageGroup, new List<ObservedPeak> { observation });
                }
            }

            this.sortedVoltageGroupDecreasingVoltage = this.basePeakMap.Keys.OrderByDescending(group => group.MeanVoltageInVolts).ToList();
            this.sortedVoltageGroupIncreasingVoltage = this.basePeakMap.Keys.OrderBy(group => group.MeanVoltageInVolts).ToList();

            // Construct the graph using the library.
            BidirectionalGraph<ObservedPeak, T> graph = new BidirectionalGraph<ObservedPeak, T>(true);

            ObservedPeak sourceVertex = new ObservedPeak();
            ObservedPeak sinkVertex = new ObservedPeak();

            // Add source and sink vertices
            graph.AddVertex(sourceVertex);
            this.SourceVertex = sourceVertex;
            graph.AddVertex(sinkVertex);
            this.SinkVertex = sinkVertex;
            
            // Add observation vertices
            graph.AddVertexRange(observations);

            // Add edges in between source/sink and observed peaks
            foreach (var vertex in graph.Vertices)
            {
                if (vertex.ObservationType != ObservationType.Virtual)
                {
                    graph.AddEdge(transitionFunction(sourceVertex, vertex));
                    graph.AddEdge(transitionFunction(vertex, sinkVertex));
                }
            }

            // Add edges in between observed peaks
            for (int i = 0; i < this.sortedVoltageGroupDecreasingVoltage.Count - 1; i++)
            {
                VoltageGroup current = this.sortedVoltageGroupDecreasingVoltage[i];
                VoltageGroup next = this.sortedVoltageGroupDecreasingVoltage[i + 1];
                foreach (var source in this.FindPeaksInVoltageGroup(current))
                {
                    foreach (var sink in this.FindPeaksInVoltageGroup(next))
                    {
                        graph.AddEdge(transitionFunction(source, sink));
                    }
                }
            }

            this.PeakGraph = graph;
        }

        public ObservedPeak SourceVertex { get; private set; } 

        public ObservedPeak SinkVertex { get; private set; } 

        /// <summary>
        /// Gets or sets the peak graph.
        /// </summary>
        public IBidirectionalGraph<ObservedPeak, T> PeakGraph { get; private set; } 

        public IEnumerable<VoltageGroup> SortedVoltageGroups(bool increasingVoltage = false)
        {
            return increasingVoltage ? this.sortedVoltageGroupIncreasingVoltage : this.sortedVoltageGroupDecreasingVoltage;        
        }

        /// <summary>
        /// The candidate peaks.
        /// </summary>
        /// <param name="voltageGroup">
        /// The voltage group.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public IEnumerable<ObservedPeak> FindPeaksInVoltageGroup(VoltageGroup voltageGroup)
        {
            if (!this.basePeakMap.ContainsKey(voltageGroup))
            {
                throw new ArgumentException("Voltage group not defined in observation transition graph");
            }

            return this.basePeakMap[voltageGroup];
        }

        public void PlotGraph()
        {
            var graphviz = new GraphvizAlgorithm<ObservedPeak, T>(this.PeakGraph);
            graphviz.Generate(new ObservationTransitionGraphRenderingEngine(), "graphDOT");
        }
    }
}
