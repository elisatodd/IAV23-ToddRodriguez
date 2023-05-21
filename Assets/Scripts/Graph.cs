namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Experimental.GraphView;
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
            //GET NEAREST VERTEX DE GRAPHGRID SOBRE srcO y dstO 
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

        public List<Vertex> GetPathMyAstar(GameObject srcO, GameObject dstO, Heuristic heuristic = null)
        {
            Vertex origin = GetNearestVertex(srcO.transform.position);
            Vertex destiny = GetNearestVertex(dstO.transform.position);

            // para comprobar que se han recorrido todas las estaciones
            List<Vertex> requiredVertices = new List<Vertex>();
            foreach (var vertex in vertices)
            {
                if (vertex.essential) { requiredVertices.Add(vertex); }
            }

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

                // cuando se llegue al destino final, se detiene la búsqueda
                if (current.vertexId == destiny.id)
                {
                    break;
                }

                // Tomamos los adyacentes al vértice actual
                connections = GetNeighbours(vertices[current.vertexId]);

                // Recorremos para cada adyecente
                foreach (Vertex connection in connections)
                {
                    // Hacemos una estimación sobre el coste de llegar desde aquí al final
                    Node endNode = new Node();
                    endNode.vertexId = connection.id;

                    float endNodeCost = current.costSoFar + connection.cost;

                    Node endNodeRecord;
                    float endNodeHeuristic;
                    // Si el nodo está cerrado, o nos lo saltamos o lo quitamos de la lista
                    if (closed.Contains(endNode))
                    {
                        endNodeRecord = closed.Find(endNode);

                        // Si no encontramos una ruta más corta para este nodo
                        if (endNodeRecord.costSoFar <= endNodeCost) { continue; }

                        // Por el contrario, lo quitamos de la lista
                        closed.Remove(endNodeRecord);

                        // Podemos usar los antiguos valores para obtener la heurística de este nodo
                        endNodeHeuristic = endNodeRecord.estimatedTotalCost - endNodeRecord.costSoFar;
                    }
                    else if (open.Contains(endNode))
                    {
                        endNodeRecord = closed.Find(endNode);

                        // Si no mejoramos la ruta, seguimos con el bucle
                        if (endNodeRecord.costSoFar <= endNodeCost)
                        { continue; }

                        endNodeHeuristic = endNodeRecord.estimatedTotalCost - endNodeRecord.costSoFar;
                    }
                    else // Aquí quedan los nodos no visitados aún
                    {
                        endNodeRecord = new Node();
                        endNodeRecord.vertexId = endNode.vertexId;

                        // Necesitamos la función heurística para poder estimar el coste al hasta el final
                        endNodeHeuristic = heuristic.Invoke(vertices[endNode.vertexId], destiny);
                    }

                    // Aquí actualizamos los costes del NodeRecord
                    endNodeRecord = new Node();
                    endNodeRecord.vertexId = endNode.vertexId;
                    endNodeRecord.costSoFar = endNodeCost;
                    endNodeRecord.prevNode = current;
                    endNodeRecord.estimatedTotalCost = endNodeCost + endNodeHeuristic;

                    // Se añade a la lista 
                    if (!open.Contains(endNodeRecord))
                        open.Push(endNodeRecord);
                }
                closed.Push(current);
            }

            Vertex currentVertex = vertices[current.vertexId];
            // Caso en el que no hemos encontrado la salida
            if (currentVertex != destiny)
            {
                Debug.Log("No es posible llegar a " + dstO.name + " desde " + srcO.name);
                return null;
            }

            // Recorremos la lista de manera inversa, viajando por los previos
            Node aux = new Node();
            List<int> inversePath = new List<int>();
            while (current.vertexId != origin.id)
            {

                inversePath.Add(current.vertexId);
                // Podemos usar un auxiliar que solo almacene el vertex id ya que es lo único que necesita para hacer el find
                current = current.prevNode;
            }

            // Construimos el camino añadiendo los nodos a la lista, empezando por el final
            return BuildPath(origin.id, destiny.id, inversePath);
        }

        /// <summary>
        /// TSPSolver
        /// </summary>
        /// 

        public List<Vertex> SolveTSP(GameObject srcO, GameObject dstO, Heuristic heuristic)
        {
            List<Vertex> goals = new List<Vertex>();
            foreach (Vertex v in vertices)
            {
                if (v.essential)
                    goals.Add(v);
            }

            // Generate all possible permutations of the goals
            List<List<Vertex>> allPermutations = GeneratePermutations(goals);

            // Initialize the shortest path and its length
            List<Vertex> shortestPath = null;
            float shortestLength = float.PositiveInfinity;

            // Iterate through each permutation
            foreach (List<Vertex> permutation in allPermutations)
            {
                // Create a modified permutation that includes the source and destination vertices
                List<Vertex> modifiedPermutation = new List<Vertex>(permutation);
                modifiedPermutation.Insert(0, GetNearestVertex(srcO.transform.position));
                modifiedPermutation.Add(GetNearestVertex(dstO.transform.position));

                // Calculate the length of the current permutation
                float length = 0;
                for (int i = 0; i < modifiedPermutation.Count - 1; i++)
                {
                    Vertex start = modifiedPermutation[i];
                    Vertex end = modifiedPermutation[i + 1];
                    List<Vertex> path = GetPathMyAstar(srcO, dstO, heuristic);
                    if (path == null)
                    {
                        length = float.PositiveInfinity; // Invalid path, set length to infinity
                        break;
                    }
                    length += CalculatePathLength(path, heuristic);
                }

                // Check if the current permutation is the shortest so far
                if (length < shortestLength)
                {
                    shortestLength = length;
                    shortestPath = modifiedPermutation;
                }
            }

            return shortestPath;
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
