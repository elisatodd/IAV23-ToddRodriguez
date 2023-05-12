namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;
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

            //int idSrc = GridTo(srcO.transform.position.x, srcO.transform.position.y);
            Node startRecord = new Node();
            startRecord.vertexId = origin.id;
            startRecord.prevNode = null;
            startRecord.costSoFar = 0;
            //startRecord.estimatedTotalCost = h.estimate(srcO);

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

                    //// Buscar cada minotauro
                    //MinoCollision[] mino = FindObjectsOfType<MinoCollision>();
                    //foreach (MinoCollision m in mino)
                    //{
                    //    // comprobar si está sobre esta casilla
                    //    if (GetNearestVertex(m.gameObject.transform.position) == connection)
                    //    {
                    //        endNodeCost += 5.0f; // valor que aumente el minotauro
                    //    }
                    //}

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
                //current = closed.Find(aux);
            }

            //Construimos el camino añadiendo los nodos a la lista, empezando por el final
            return BuildPath(origin.id, destiny.id, inversePath);
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
        private List<Vertex> BuildPath(int srcId, int dstId, ref int[] prevList)
        {
            List<Vertex> path = new List<Vertex>();

            if (dstId < 0 || dstId >= vertices.Count)
                return path;

            int prev = dstId;
            do
            {
                path.Add(vertices[prev]);
                prev = prevList[prev];
            } while (prev != srcId);
            return path;
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
