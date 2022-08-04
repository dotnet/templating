﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Utils
{
    public class DirectedGraph<T>
    {
        private static readonly DirectedGraph<T> Empty = new DirectedGraph<T>(new Dictionary<T, HashSet<T>>());

        private readonly Dictionary<T, HashSet<T>> _dependenciesMap;
        private readonly Lazy<Dictionary<T, HashSet<T>>> _dependantsMap;
        private readonly IReadOnlyList<T> _vertices;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectedGraph{T}"/> class.
        /// </summary>
        /// <param name="dependenciesMap">Lookup of child nodes for all nodes (or at lest nodes that have children).</param>
        public DirectedGraph(Dictionary<T, HashSet<T>> dependenciesMap)
        {
            _dependenciesMap = DeepCopy(dependenciesMap);
            _vertices = GetVertices(_dependenciesMap);
            _dependantsMap = new Lazy<Dictionary<T, HashSet<T>>>(() => GetDependandsMap(_dependenciesMap, _vertices));
        }

        internal IReadOnlyDictionary<T, HashSet<T>> DependenciesMap => _dependenciesMap;

        private bool IsEmpty => _dependenciesMap.Count == 0;

        public static implicit operator DirectedGraph<T>(Dictionary<T, HashSet<T>> dependenciesMap) => new DirectedGraph<T>(dependenciesMap);

        /// <summary>
        /// Attempts to perform a topological sort of a given acyclic graph.
        /// </summary>
        /// <returns>True if topological sort can be performed, false otherwise (means graph contains cycle(s)).</returns>
        public bool TryGetTopologicalSort(out IReadOnlyList<T> sortedElements)
        {
            List<T> result = new List<T>();
            sortedElements = result;

            // short circuit for empty graph
            if (IsEmpty)
            {
                return true;
            }

            var inDegreeLookup = _vertices.ToDictionary(v => v, v => 0);
            foreach (var depPair in _dependenciesMap)
            {
                inDegreeLookup[depPair.Key] = depPair.Value?.Count ?? 0;
            }

            Queue<T> noDependenciesQueue = new Queue<T>(inDegreeLookup.Where(kp => kp.Value == 0).Select(kp => kp.Key));
            var dependantsMap = _dependantsMap.Value;

            while (noDependenciesQueue.Count != 0)
            {
                T item = noDependenciesQueue.Dequeue();
                result.Add(item);

                foreach (T dependant in dependantsMap[item])
                {
                    if (--inDegreeLookup[dependant] == 0)
                    {
                        noDependenciesQueue.Enqueue(dependant);
                    }
                }
            }

            // if we haven't traverse everything then cycle exist in given graph
            return result.Count == _vertices.Count;
        }

        public IEnumerable<T> GetDependands(IEnumerable<T> vertices)
        {
            var dependantsMap = _dependantsMap.Value;
            return vertices.Select(v => dependantsMap[v]).SelectMany(v => v);
        }

        /// <summary>
        /// Gets the subset of the graph that contains all the dependencies mapping depending on given vertices.
        /// This is useful when we want to determine a subset of the graph that needs to be reevaluated if given nodes change value.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="includeSeedVertices">
        /// Indication whether the given vertices should be included in the output graph.
        /// If set to false, (some of) the given nodes may still be part of output graph, in case they transitively depend on any node from input set.
        /// </param>
        /// <returns></returns>
        public DirectedGraph<T> GetSubGraphDependandOnVertices(IReadOnlyList<T> vertices, bool includeSeedVertices = false)
        {
            // Short circuit for empty graphs
            if (IsEmpty || vertices.Count == 0)
            {
                return Empty;
            }

            HashSet<T> dependantVertices = includeSeedVertices ? new HashSet<T>(vertices) : new HashSet<T>();
            Queue<T> directDependants = new Queue<T>(vertices);
            var dependantsMap = _dependantsMap.Value;

            while (directDependants.Count > 0)
            {
                T parent = directDependants.Dequeue();
                directDependants.AddRange(dependantsMap[parent].Where(dependantVertices.Add));
            }

            return _dependenciesMap.Where(p => dependantVertices.Contains(p.Key))
                .ToDictionary(p => p.Key, p => new HashSet<T>(p.Value.Where(dependantVertices.Contains)));
        }

        /// <summary>
        /// Detects a cycle in directed (possibly disconnected) graph and returns first found cycle in cycle variable.
        /// </summary>
        /// <param name="cycle">First cycle if any found.</param>
        /// <returns>True if cycle found, false otherwise.</returns>
        public bool HasCycle(out IReadOnlyList<T> cycle)
        {
            cycle = new List<T>();

            if (IsEmpty)
            {
                return false;
            }

            HashSet<T> visitedVertices = new HashSet<T>();
            RecursionStack recursionStack = new RecursionStack();

            // detect cycles for any vertex (as we can have disconnected graph here)
            foreach (T vertex in _vertices)
            {
                if (IsCyclicUtil(vertex, visitedVertices, recursionStack))
                {
                    cycle = recursionStack.GetCycle();
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<T> GetVertices(Dictionary<T, HashSet<T>> dependenciesMap)
            => dependenciesMap.Keys.Union(dependenciesMap.Values.SelectMany(v => v)).Distinct().ToList();

        private static Dictionary<T, HashSet<T>> GetDependandsMap(Dictionary<T, HashSet<T>> dependenciesMap, IReadOnlyList<T> vertices)
        {
            var dependantsMap = vertices.ToDictionary(v => v, v => new HashSet<T>());
            foreach (KeyValuePair<T, HashSet<T>> keyValuePair in dependenciesMap)
            {
                foreach (T dependency in keyValuePair.Value!)
                {
                    dependantsMap[dependency].Add(keyValuePair.Key);
                }
            }

            return dependantsMap;
        }

        private static Dictionary<T, HashSet<T>> DeepCopy(Dictionary<T, HashSet<T>> dependenciesMap)
        {
            return dependenciesMap
                .ToDictionary(kp => kp.Key, kp => kp.Value == null ? new() : new HashSet<T>(kp.Value));
        }

        private bool IsCyclicUtil(T vertex, HashSet<T> visitedVertices, RecursionStack recursionStack)
        {
            // Mark the current node as visited and part of recursion stack
            if (!recursionStack.TryPush(vertex))
            {
                return true;
            }

            if (!visitedVertices.Add(vertex))
            {
                recursionStack.Pop();
                return false;
            }

            if (_dependenciesMap.TryGetValue(vertex, out var children) && children != null && children.Count != 0)
            {
                foreach (T child in children)
                {
                    if (IsCyclicUtil(child, visitedVertices, recursionStack))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Pop();

            return false;
        }

        private class RecursionStack
        {
            private readonly HashSet<T> _lookup = new HashSet<T>();
            private readonly Stack<T> _stack = new Stack<T>();

            public bool TryPush(T item)
            {
                _stack.Push(item);
                return _lookup.Add(item);
            }

            public void Pop()
            {
                _lookup.Remove(_stack.Pop());
            }

            public IReadOnlyList<T> GetCycle()
            {
                List<T> items = new List<T>();
                HashSet<T> visited = new HashSet<T>();

                bool hasCycle = false;

                while (_stack.Count != 0)
                {
                    T current = _stack.Pop();
                    items.Add(current);
                    if (!visited.Add(current))
                    {
                        hasCycle = true;
                        break;
                    }
                }

                if (hasCycle)
                {
                    items.Reverse();
                }
                else
                {
                    items.Clear();
                }

                return items;
            }
        }
    }
}
