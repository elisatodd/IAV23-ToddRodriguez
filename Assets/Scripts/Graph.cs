namespace IAV23.ElisaTodd
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public abstract class Graph : MonoBehaviour
    {
        public GameObject vertexPrefab;
        protected List<Vertex> vertices;
        protected List<List<Vertex>> neighbourVertex;
        protected List<List<float>> costs;
        protected bool[,] mapVertices;
        protected float[,] costsVertices;
        protected int numCols, numRows;

        public delegate float Heuristic(Vertex a, Vertex b);

        public List<Vertex> path;

        // Eventos para avisar de mapa no válido
        public delegate void InvalidMapDelegate(bool unreachable);
        public static event InvalidMapDelegate InvalidMap;

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

        /// <summary>
        /// Método A* original que busca el camino óptimo entre 2 puntos
        /// </summary>
        /// <param name="srcO"> Objeto del que parte el camino </param>
        /// <param name="dstO"> Objeto al que llega el camino </param>
        /// <param name="h"> Heurística que utiliza para valorar la distancia entre 2 puntos </param>
        /// <returns> Una lista con los vértices que componen el camino </returns>
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

        /// <summary>
        /// Método A* modificado que busca el camino óptimo entre 2 puntos, usando un combustible 
        /// que se va agotando o recargando a medida que recorre el camino
        /// 
        /// </summary>
        /// <param name="srcO"> Objeto del que parte el camino </param>
        /// <param name="dstO"> Objeto al que llega el camino </param>
        /// <param name="gas"> Combustible que tiene para realizar el recorrido </param>
        /// <param name="h"> Heurística que utiliza para valorar la distancia entre 2 puntos </param>
        /// <returns> Una lista con los vértices que componen el camino </returns>
        public List<Vertex> GetPathMyAstar(GameObject srcO, GameObject dstO, ref float gas, Heuristic h = null)
        {
            if (gas <= 0)
            {
                Debug.Log("La gasolina inicial no es suficiente para completar el camino.");
                return null;
            }

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
                        endNodeHeuristic = endNodeRecord.estimatedTotalCost - endNodeRecord.costSoFar; //h = f - g ¿es necesario?¿A dónde va endNodeHeuristic?

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
                        endNodeHeuristic = h.Invoke(vertices[endNode.vertexId], destiny);
                    }

                    // Aquí actualizamos los costes del NodeRecord
                    endNodeRecord = new Node();
                    endNodeRecord.vertexId = endNode.vertexId;
                    endNodeRecord.costSoFar = endNodeCost;
                    endNodeRecord.prevNode = current;
                    endNodeRecord.estimatedTotalCost = endNodeCost + endNodeHeuristic;

                    // Se añade a la lista 
                    if (!open.Contains(endNodeRecord))
                    {
                        open.Push(endNodeRecord);
                    }
                }

                // Al visitar las conexiones de este nodo, lo podemos
                // añadir a la lista de closed
                closed.Push(current);
            }

            Vertex currentVertex = vertices[current.vertexId];
            // Caso en el que no hemos encontrado la salida
            if (currentVertex != destiny)
            {
                //Debug.Log("No se puede alcanzar la salida " + dstO.name + " desde " + srcO.name);
                return null;
            }

            // Recorremos la lista de manera inversa, viajando por los previos
            Node aux = new Node();
            List<int> inversePath = new List<int>();
            while (current.vertexId != origin.id)
            {
                inversePath.Add(current.vertexId);
                current = current.prevNode;

                // El combustible se gasta al moverse a la casilla
                currentGas -= vertices[current.vertexId].cost;
                // Y se puede recargar con el valor que la casilla indique (caso de los bidones)
                currentGas += vertices[current.vertexId].gas;

                if (currentGas < 1) { ranOut = true; break; }
            }

            // Caso de gasolina insuficiente
            if (ranOut)
            {
                //Debug.Log("La gasolina no es suficiente para completar este camino");
                return null;
            }

            gas = currentGas; // Actualizar el valor de la gasolina disponible

            // Construimos el camino añadiendo los nodos a la lista, empezando por el final
            return BuildPath(origin.id, destiny.id, inversePath);
        }

        /// <summary>
        /// Fijándose en el algoritmo del problema del vendedor ambulante, devuelve la lista de nodos
        /// que componen la solución al problema de ir de un inicio a un final, pasando por unos
        /// nodos obligatoriamente y usando combustible que se puede agotar
        /// </summary>
        /// <param name="srcO"> Objeto del que parte el camino </param>
        /// <param name="dstO"> Objeto al que llega el camino </param>
        /// <param name="heuristic"> Heurística que utiliza para valorar la distancia entre 2 puntos</param>
        /// <returns></returns>
        public List<Vertex> SolveTSP(GameObject srcO, GameObject dstO, Heuristic heuristic)
        {
            Vertex origin = GetNearestVertex(srcO.transform.position);
            Vertex destiny = GetNearestVertex(dstO.transform.position);

            // Guarda los vértices que son imprescindibles: las estaciones
            List<Vertex> essentialVertices = new List<Vertex>();
            foreach (Vertex v in vertices)
            {
                if (v.essential)
                    essentialVertices.Add(v);
            }

            // Se generan todas las permutaciones o variaciones posibles con esos vértices
            List<List<Vertex>> essentialPermutations = GeneratePermutations(essentialVertices);

            // Se crean variables para guardar el mejor camino encontrado
            List<Vertex> shortestPath = null;
            float shortestLength = float.PositiveInfinity;

            bool allEssentialVisited = false; // Variable para verificar si se visitaron todos los nodos esenciales

            // Iterar por cada permutación posible
            foreach (List<Vertex> permutation in essentialPermutations)
            {
                float gasoline = GameManager.instance.GasInitialLevel;

                // Se deben incluir los vértices de entrada y salida,
                // siempre en la primera y última posición
                permutation.Insert(0, origin);
                permutation.Add(destiny);

                float length = 0;
                List<Vertex> completePath = new List<Vertex>();

                // Iterar por cada vértice de la permutación
                for (int i = 0; i < permutation.Count - 1; i++)
                {
                    Vertex start = permutation[i];
                    Vertex end = permutation[i + 1];

                    // Verificar gasolina disponible antes de calcular el camino
                    if (gasoline <= 0)
                    {
                        length = float.PositiveInfinity; // Camino inválido, se queda sin gasolina
                        break;
                    }

                    // Se calcula la distancia entre cada vértice desde la permutación
                    List<Vertex> path = GetPathMyAstar(start.gameObject, end.gameObject, ref gasoline, heuristic);

                    if (path == null)
                    {
                        length = float.PositiveInfinity; // Camino no válido
                        break;
                    }

                    path.Reverse();

                    // Añadir los vértices a la solución
                    completePath.AddRange(path.GetRange(0, path.Count));

                    length += CalculatePathLength(path, heuristic);
                }

                // Comprueba si la permutación calculada es mejor solución
                bool visitedAll = (length < float.PositiveInfinity);
                if (visitedAll && length < shortestLength)
                {
                    shortestLength = length;
                    shortestPath = completePath;
                    allEssentialVisited = true; 
                }
            }

            // Si no se han visitado todos los vértices esenciales
            if (!allEssentialVisited)
            {
                Debug.Log("El mapa es inválido --> no se pudieron visitar todas las estaciones");
            }
            else
            {
                Debug.Log("El mapa es válido --> se pueden visitar todas las estaciones");
            }

            if (shortestPath != null)
            {
                shortestPath.Insert(0, origin);
                return shortestPath;
            }
            else
            {
                // Si no se ha conseguido completar el camino
                InvalidMap(allEssentialVisited); // pasa por param. el motivo de la invalidez
                return null;
            }
        }

        /// <summary>
        /// Calcula la longitud de un camino según lo que determine la heurística
        /// </summary>
        /// <param name="path"> Camino del que se quiere saber la longitud </param>
        /// <param name="heuristic"> Heurística para determinar la longitud </param>
        /// <returns> Devuelve la longitud del camino </returns>
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

        /// <summary>
        /// Genera todas las permutaciones posibles dada una lista de vértices
        /// </summary>
        /// <param name="goals"> Vértices que se quieren combinar </param>
        /// <returns> Lista de listas de vértices, ordenada cada una de forma distinta </returns>
        private List<List<Vertex>> GeneratePermutations(List<Vertex> goals)
        {
            List<List<Vertex>> permutations = new List<List<Vertex>>();
            GeneratePermutationsHelper(goals, 0, permutations);
            return permutations;
        }

        /// <summary>
        /// Método recursivo para calcular las permutaciones posibles dada una lista de vértices
        /// </summary>
        /// <param name="goals"> vértices que se tienen que permutar </param>
        /// <param name="start"> índice del primer vértice de la permutación que se va a generar ahora </param>
        /// <param name="permutations"> permutaciones generadas hasta el momento </param>
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

        /// <summary>
        /// Intercambia las posiciones de dos elementos dentro de una lista
        /// </summary>
        /// <param name="goals"> lista que se quiere modificar </param>
        /// <param name="i"> posición de uno de los elementos a intercambiar </param>
        /// <param name="j"> posición de uno de los elementos a intercambiar </param>
        private void Swap(List<Vertex> goals, int i, int j)
        {
            Vertex temp = goals[i];
            goals[i] = goals[j];
            goals[j] = temp;
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
