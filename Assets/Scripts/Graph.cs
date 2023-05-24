namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Experimental.GraphView;
    using UnityEditor.MemoryProfiler;
    using UnityEngine;
    using UnityEngine.UIElements;
    using static IAV23.ElisaTodd.Graph;
    using static UnityEngine.Rendering.DebugUI;

    public abstract class Graph : MonoBehaviour
    {
        public GameObject vertexPrefab;
        protected List<Vertex> vertices;
        protected List<List<Vertex>> neighbourVertex;
        protected List<List<float>> costs;
        protected bool[,] mapVertices;
        protected float[,] costsVertices;
        protected int numCols, numRows;

        // this is for informed search like A*
        public delegate float Heuristic(Vertex a, Vertex b);

        // Used for getting path in frames
        public List<Vertex> path;

        public virtual void Start()
        {
            Load();
        }

        public virtual void Load() { }

        public virtual int GetSize()
        {
            if (ReferenceEquals(vertices, null))
                return 0;
            return vertices.Count;
        }

        public virtual void UpdateVertexCost(Vector3 position, float costMultiplier) { }

        public virtual Vertex GetNearestVertex(Vector3 position)
        {
            return null;
        }

        public virtual GameObject GetRandomPos()
        {
            return null;
        }

        public virtual Vertex[] GetNeighbours(Vertex v)
        {
            if (ReferenceEquals(neighbourVertex, null) || neighbourVertex.Count == 0 ||
                v.id < 0 || v.id >= neighbourVertex.Count)
                return new Vertex[0];
            return neighbourVertex[v.id].ToArray();
        }

        public virtual float[] GetNeighboursCosts(Vertex v)
        {
            if (ReferenceEquals(neighbourVertex, null) || neighbourVertex.Count == 0 ||
                v.id < 0 || v.id >= neighbourVertex.Count)
                return new float[0];

            Vertex[] neighs = neighbourVertex[v.id].ToArray();
            float[] costsV = new float[neighs.Length];
            for (int neighbour = 0; neighbour < neighs.Length; neighbour++)
            {
                int j = (int)Mathf.Floor(neighs[neighbour].id / numCols);
                int i = (int)Mathf.Floor(neighs[neighbour].id % numCols);
                costsV[neighbour] = costsVertices[j, i];
            }

            return costsV;
        }

        public List<Vertex> GetPathAstar(GameObject srcO, GameObject dstO, Heuristic h = null)
        {
            Vertex origin = GetNearestVertex(srcO.transform.position);
            Vertex destiny = GetNearestVertex(dstO.transform.position);

            Node startRecord = new Node();
            startRecord.vertexId = origin.id;
            startRecord.prevNode = null;
            startRecord.costSoFar = 0;

            PriorityQueue<Node> open = new PriorityQueue<Node>();
            open.Push(startRecord);
            PriorityQueue<Node> closed = new PriorityQueue<Node>();
            Node current = startRecord;
            Vertex[] connections;
            while (!open.Empty() && open.Top() != null)
            {
                current = open.Top();
                open.Pop();
                if (current.vertexId == destiny.id)
                {
                    break;
                }

                //Tomamos los adyacentes al vértice actual
                connections = GetNeighbours(vertices[current.vertexId]);

                //Recorremos para cada adyecente
                foreach (Vertex connection in connections)
                {
                    //Hacemos una estimación sobre el coste de llegar desde aquí al final
                    Node endNode = new Node();
                    endNode.vertexId = connection.id;

                    float endNodeCost = current.costSoFar + connection.cost;

                    Node endNodeRecord;
                    float endNodeHeuristic;
                    //Si el nodo está cerrado, o nos lo saltamos o lo quitamos de la lista
                    if (closed.Contains(endNode))
                    {
                        endNodeRecord = closed.Find(endNode);

                        //Si no encontramos una ruta más corta para este nodo
                        if (endNodeRecord.costSoFar <= endNodeCost) { continue; }

                        //Por el contrario, lo quitamos de la lista
                        closed.Remove(endNodeRecord);

                        //Podemos usar los antiguos valores para obtener la heurística de este nodo
                        endNodeHeuristic = endNodeRecord.estimatedTotalCost - endNodeRecord.costSoFar; //h = f - g ¿es necesario?¿A dónde va endNodeHeuristic?
                    }
                    else if (open.Contains(endNode))
                    {
                        endNodeRecord = closed.Find(endNode);

                        //Si no mejoramos la ruta, seguimos con el bucle
                        if (endNodeRecord.costSoFar <= endNodeCost)
                        { continue; }

                        endNodeHeuristic = endNodeRecord.estimatedTotalCost - endNodeRecord.costSoFar;
                    }
                    else //Aquí quedan los nodos no visitados aún
                    {
                        endNodeRecord = new Node();
                        endNodeRecord.vertexId = endNode.vertexId;

                        //Necesitamos la función heurística para poder estimar el coste al hasta el final
                        endNodeHeuristic = h.Invoke(vertices[endNode.vertexId], destiny);


                    }

                    //Aquí actualizamos los costes del NodeRecord
                    endNodeRecord = new Node();
                    endNodeRecord.vertexId = endNode.vertexId;
                    endNodeRecord.costSoFar = endNodeCost;
                    endNodeRecord.prevNode = current;
                    endNodeRecord.estimatedTotalCost = endNodeCost + endNodeHeuristic;

                    //Se añade a la lista 
                    if (!open.Contains(endNodeRecord))
                        open.Push(endNodeRecord);
                }
                //Al visitar las conexiones de este nodo, lo podemos
                //añadir a la lista de closed

                closed.Push(current);
            }

            Vertex currentVertex = vertices[current.vertexId];
            //Caso en el que no hemos encontrado la salida
            if (currentVertex != destiny)
            {
                return null;
            }

            //Recorremos la lista de manera inversa, viajando por los previos
            Node aux = new Node();
            List<int> inversePath = new List<int>();
            while (current.vertexId != origin.id)
            {
                inversePath.Add(current.vertexId);
                //Podemos usar un auxiliar que solo almacene el vertex id ya que es lo único que necesita para hacer el find
                current = current.prevNode;
            }

            //Construimos el camino añadiendo los nodos a la lista, empezando por el final
            return BuildPath(origin.id, destiny.id, inversePath);
        }

        public List<Vertex> GetPathMyAstar(GameObject srcO, GameObject dstO, ref float gas, Heuristic h = null)
        {
            float currentGas = gas;
            bool ranOut = false;

            Vertex origin = GetNearestVertex(srcO.transform.position);
            Vertex destiny = GetNearestVertex(dstO.transform.position);

            Node startRecord = new Node();
            startRecord.vertexId = origin.id;
            startRecord.prevNode = null;
            startRecord.costSoFar = 0;

            PriorityQueue<Node> open = new PriorityQueue<Node>();
            open.Push(startRecord);
            PriorityQueue<Node> closed = new PriorityQueue<Node>();
            Node current = startRecord;
            Vertex[] connections;
            while (!open.Empty() && open.Top() != null)
            {
                current = open.Top();
                open.Pop();
                if (current.vertexId == destiny.id)
                {
                    break;
                }

                //Tomamos los adyacentes al vértice actual
                connections = GetNeighbours(vertices[current.vertexId]);

                //Recorremos para cada adyecente
                foreach (Vertex connection in connections)
                {
                    //Hacemos una estimación sobre el coste de llegar desde aquí al final
                    Node endNode = new Node();
                    endNode.vertexId = connection.id;

                    float endNodeCost = current.costSoFar + connection.cost;

                    Node endNodeRecord;
                    float endNodeHeuristic;
                    //Si el nodo está cerrado, o nos lo saltamos o lo quitamos de la lista
                    if (closed.Contains(endNode))
                    {
                        endNodeRecord = closed.Find(endNode);

                        //Si no encontramos una ruta más corta para este nodo
                        if (endNodeRecord.costSoFar <= endNodeCost) { continue; }

                        //Por el contrario, lo quitamos de la lista
                        closed.Remove(endNodeRecord);

                        //Podemos usar los antiguos valores para obtener la heurística de este nodo
                        endNodeHeuristic = endNodeRecord.estimatedTotalCost - endNodeRecord.costSoFar; //h = f - g ¿es necesario?¿A dónde va endNodeHeuristic?

                    }
                    else if (open.Contains(endNode))
                    {
                        endNodeRecord = closed.Find(endNode);

                        //Si no mejoramos la ruta, seguimos con el bucle
                        if (endNodeRecord.costSoFar <= endNodeCost)
                        { continue; }

                        endNodeHeuristic = endNodeRecord.estimatedTotalCost - endNodeRecord.costSoFar;

                    }
                    else //Aquí quedan los nodos no visitados aún
                    {
                        endNodeRecord = new Node();
                        endNodeRecord.vertexId = endNode.vertexId;

                        //Necesitamos la función heurística para poder estimar el coste al hasta el final
                        endNodeHeuristic = h.Invoke(vertices[endNode.vertexId], destiny);

                    }

                    //Aquí actualizamos los costes del NodeRecord
                    endNodeRecord = new Node();
                    endNodeRecord.vertexId = endNode.vertexId;
                    endNodeRecord.costSoFar = endNodeCost;
                    endNodeRecord.prevNode = current;
                    endNodeRecord.estimatedTotalCost = endNodeCost + endNodeHeuristic;

                    //Se añade a la lista 
                    if (!open.Contains(endNodeRecord))
                    {
                        open.Push(endNodeRecord);
                    }
                }

                //Al visitar las conexiones de este nodo, lo podemos
                //añadir a la lista de closed
                closed.Push(current);

            }

            Vertex currentVertex = vertices[current.vertexId];
            //Caso en el que no hemos encontrado la salida
            if (currentVertex != destiny)
            {
                Debug.Log("No se puede alcanzar la salida " + dstO.name + " desde " + srcO.name);
                return null;
            }

            //Recorremos la lista de manera inversa, viajando por los previos
            Node aux = new Node();
            List<int> inversePath = new List<int>();
            while (current.vertexId != origin.id)
            {
                inversePath.Add(current.vertexId);
                //Podemos usar un auxiliar que solo almacene el vertex id ya que es lo único que necesita para hacer el find
                current = current.prevNode;

                currentGas -= vertices[current.vertexId].cost;
                currentGas += vertices[current.vertexId].gas;

                if (currentGas < 1) { ranOut = true; break; }
            }

            // Caso de gasolina insuficiente
            if (ranOut)
            {
                Debug.Log("La gasolina no es suficiente para completar este nivel");
                return null;
            }
            
            Debug.Log("Finaliza con " + currentGas + " de gasolina.");
            //Construimos el camino añadiendo los nodos a la lista, empezando por el final
            return BuildPath(origin.id, destiny.id, inversePath);
        }
        public List<Vertex> SolveTSP(GameObject srcO, GameObject dstO, Heuristic heuristic)
        {
            Vertex origin = GetNearestVertex(srcO.transform.position);
            Vertex destiny = GetNearestVertex(dstO.transform.position);

            List<Vertex> essentialVertices = new List<Vertex>();
            foreach (Vertex v in vertices)
            {
                if (v.essential)
                    essentialVertices.Add(v);
            }

            // Generate all possible permutations of the essential vertices
            List<List<Vertex>> essentialPermutations = GeneratePermutations(essentialVertices);

            // Initialize the shortest path and its length
            List<Vertex> shortestPath = null;
            float shortestLength = float.PositiveInfinity;

            // Iterate through each permutation
            foreach (List<Vertex> permutation in essentialPermutations)
            {
                // Include the source and destination vertices
                permutation.Insert(0, origin);
                permutation.Add(destiny);

                // Calculate the length of the current permutation
                float length = 0;
                List<Vertex> completePath = new List<Vertex>();

                List<bool> visitedStation = new List<bool>();

                // Iterate through each vertex in the modified permutation
                for (int i = 0; i < permutation.Count - 1; i++)
                {
                    Vertex start = permutation[i];
                    Vertex end = permutation[i + 1];

                    // Get the path between the current start and end vertices
                    float gasoline = GameManager.instance.GasLevel;
                    List<Vertex> path = GetPathMyAstar(start.gameObject, end.gameObject, ref gasoline, heuristic);
                    path.Reverse();

                    if (path == null)
                    {
                        length = float.PositiveInfinity; // Invalid path, set length to infinity
                        visitedStation[i] = false;
                        break;
                    }

                    visitedStation[i] = true;

                    // Add the vertices to the solution
                    completePath.AddRange(path.GetRange(0, path.Count));

                    length += CalculatePathLength(path, heuristic);
                }

                // Check if the current permutation is the shortest so far
                bool visitedAll = visitedStation.All(b => b == true);
                if (visitedAll && length < shortestLength)
                {
                    shortestLength = length;
                    shortestPath = completePath;
                }
            }

            shortestPath.Insert(0, origin);
            return shortestPath;
        }
        public List<Vertex> SolveTSPOptimized(GameObject srcO, GameObject dstO, Heuristic heuristic)
        {
            Vertex origin = GetNearestVertex(srcO.transform.position);
            Vertex destiny = GetNearestVertex(dstO.transform.position);

            List<Vertex> essentialVertices = new List<Vertex>();
            List<Vertex> additionalVertices = new List<Vertex>();
            foreach (Vertex v in vertices)
            {
                if (v.essential)
                    essentialVertices.Add(v);
                else if (v != origin && v != destiny)
                    additionalVertices.Add(v);
            }

            // Initialize the shortest path and its length
            List<Vertex> bestPath = null;
            float bestLength = float.PositiveInfinity;

            // Iterate through each permutation
            foreach (Vertex initialVertex in essentialVertices)
            {
                List<Vertex> currentRoute = new List<Vertex>() { origin, initialVertex, destiny };
                float currentLength = CalculatePathLength(currentRoute, heuristic);

                foreach (Vertex vertex in essentialVertices)
                {
                    if (!currentRoute.Contains(vertex))
                    {
                        float bestInsertionLength = float.PositiveInfinity;
                        int bestPosition = -1;

                        for (int i = 1; i < currentRoute.Count; i++)
                        {
                            List<Vertex> tempRoute = new List<Vertex>(currentRoute);
                            tempRoute.Insert(i, vertex);

                            float insertionLength = CalculatePathLength(tempRoute, heuristic);

                            if (insertionLength < bestInsertionLength)
                            {
                                bestInsertionLength = insertionLength;
                                bestPosition = i;
                            }
                        }

                        if (bestPosition != -1)
                            currentRoute.Insert(bestPosition, vertex);
                    }
                }

                if (currentLength < bestLength)
                {
                    bestLength = currentLength;
                    bestPath = currentRoute;
                }
            }
            bestPath.Insert(0, origin);
            return bestPath;
        }

        private float CalculatePathLength(List<Vertex> path, Heuristic heuristic)
        {
            float length = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vertex start = path[i];
                Vertex end = path[i + 1];
                float distance = heuristic.Invoke(start, end);
                length += distance;
            }
            return length;
        }
        private List<List<Vertex>> GeneratePermutations(List<Vertex> goals)
        {
            List<List<Vertex>> permutations = new List<List<Vertex>>();
            GeneratePermutationsHelper(goals, 0, permutations);
            return permutations;
        }
        private void GeneratePermutationsHelper(List<Vertex> goals, int start, List<List<Vertex>> permutations)
        {
            if (start == goals.Count - 1)
            {
                permutations.Add(goals.ToList());
            }
            else
            {
                for (int i = start; i < goals.Count; i++)
                {
                    Swap(goals, start, i);
                    GeneratePermutationsHelper(goals, start + 1, permutations);
                    Swap(goals, start, i);
                }
            }
        }
        private void Swap(List<Vertex> goals, int i, int j)
        {
            Vertex temp = goals[i];
            goals[i] = goals[j];
            goals[j] = temp;
        }
        public List<Vertex> Smooth(List<Vertex> inputPath)
        {
            // IMPLEMENTAR SUAVIZADO DE CAMINOS, MODIFICADO

            //Si el camino tiene solo dos nodos, regresamos
            if (inputPath.Count <= 2)
                return inputPath;

            List<Vertex> outputPath = new List<Vertex>();
            outputPath.Add(inputPath[0]);

            // Keep track of where we are in the input path. We start at 2,
            // because we assume two adjacent nodes will pass the ray cast.
            int inputIndex = 2;
            RaycastHit hit;
            //iteramos hasta encontrar el último item de inputPath
            while (inputIndex < inputPath.Count - 1)
            {
                //Hacemos el raycast
                Vector3 fromPt = outputPath[outputPath.Count - 1].transform.position;
                fromPt += new Vector3(0.0f, 3.0f, 0.0f);
                Vector3 toPt = inputPath[inputIndex].transform.position;
                Ray r = new Ray(fromPt, toPt - fromPt);
                //Si el raycast no ha colisionado con otro vértice
                if (!Physics.Raycast(r, out hit, 10))
                {

                    //Al fallar el raycast, hay que añadir el último nodo a la output list
                    outputPath.Add(inputPath[inputIndex - 1]);
                }
                else if (hit.transform.gameObject.layer != outputPath[0].gameObject.layer)
                {
                    outputPath.Add(inputPath[inputIndex - 1]);
                }
                inputIndex++;
            }
            outputPath.Add(inputPath[inputPath.Count - 1]);

            return outputPath;
        }
        // Reconstruir el camino, dando la vuelta a la lista de nodos 'padres' /previos que hemos ido anotando
        private List<Vertex> BuildPath(int srcId, int dstId, List<int> prevList)
        {
            List<Vertex> path = new List<Vertex>();

            if (dstId < 0 || dstId >= vertices.Count)
                return path;

            int prev = 0;
            while (prev < prevList.Count)
            {
                //Recorre toda la lista de previos añadiendo los vértices
                path.Add(vertices[prevList[prev]]);
                prev++;
            }
            return path;
        }
    }
}
