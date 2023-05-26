namespace IAV23.ElisaTodd
{
    using UnityEngine;
    using System.Collections.Generic;
    using System;
    using System.Collections;


    // Posibles algoritmos para buscar caminos en grafos
    public enum TesterGraphAlgorithm
    {
        ASTAR
    }

    // Calcula el camino más corto entre dos puntos dado un grafo. Proporciona varias heurísticas
    public class TrainGraph : MonoBehaviour
    {
        // referencia al mapa
        [SerializeField] private GraphGrid graph;
        // referencia al jugador
        [SerializeField] private GameObject start;
        [SerializeField] private MoveTrain moveTrain;

        // algoritmo que se utiliza para resolver
        [SerializeField] private TesterGraphAlgorithm algorithm;

        [SerializeField] private string vertexTag = "Vertex"; // Etiqueta de un nodo normal

        [SerializeField] private string obstacleTag = "Obstacle"; // Etiqueta de un nodo obstáculo (rocas, casas y árboles)

        [SerializeField] private Color pathColor;
        [SerializeField]
        [Range(0.1f, 1f)]
        private float pathNodeRadius = .3f;

        protected bool reUpdatePath;

        bool firstHeuristic = true;

        Camera mainCamera;

        protected GameObject srcObj;
        protected GameObject dstObj;

        protected List<Vertex> path; // La variable con el camino calculado

        protected LineRenderer hilo;
        protected float hiloOffset = 0.2f;

        private bool simulationActive = false;

        public virtual void Awake()
        {
            mainCamera = Camera.main;
            srcObj = start;
            dstObj = GameObject.Find("ExitSlab");
            path = new List<Vertex>();
            hilo = GetComponent<LineRenderer>();
            reUpdatePath = false;

            hilo.startWidth = 0.15f;
            hilo.endWidth = 0.15f;
            hilo.positionCount = 0;
        }

        public virtual void Update()
        {
            // Si se pulsa el espacio, mostrar el camino calculado
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!reUpdatePath)
                {
                    UpdatePath(true);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // realizar simulación
                if (!reUpdatePath) // si no se hubiera mostrado el camino antes, hay que activarlo
                {
                    UpdatePath(true);
                }

                if (!simulationActive)
                {
                    simulationActive = true; 
                    StartCoroutine(moveTrain.Move(path));
                }
            }
            else
            {
                if (reUpdatePath)
                    UpdatePath(false);
            }

            // Solo lo calculamos al hacer click derecho con el ratón
            if (reUpdatePath)
            {
                CalculatePath();
            }

            if (!simulationActive)
            {
                DibujaHilo();   
            }
        }

        public void ChangeMaterial(Material m)
        {
            hilo.material = m;
        }

        public void CalculatePath()
        {
            //Source jugador y destino el nodo final
            if (srcObj == null) srcObj = GameManager.instance.GetPlayer();
            if (dstObj == null) dstObj = GameManager.instance.GetExitNode();

            switch (algorithm)
            {
                case TesterGraphAlgorithm.ASTAR:
                    path = graph.SolveTSP(srcObj, dstObj, Manhattan);
                    break;
                default: break;
            }
        }

        public float Euclidea(Vertex a, Vertex b)
        {
            return (a.transform.position - b.transform.position).magnitude;
        }

        public float Manhattan(Vertex a, Vertex b)
        {
            return (Math.Abs(a.transform.position.x - b.transform.position.x))
                  + Math.Abs(a.transform.position.z - b.transform.position.z);
        }

        public virtual Transform GetNearestNode()
        {
            if (path.Count > 0)
                return path[path.Count - 1].transform;

            return null;
        }

        public virtual Transform GetNextNode()
        {
            if (path.Count > 0)
                path.Remove(path[path.Count - 1]);

            return GetNearestNode();
        }

        public int PathSize()
        {
            return path.Count;
        }

        public Transform GetDestiny()
        {
            return graph.GetNearestVertex(dstObj.transform.position).transform;
        }

        // Dibujado de artilugios en el editor
        virtual public void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (ReferenceEquals(graph, null))
                return;

            Vertex v;
            if (!ReferenceEquals(srcObj, null))
            {
                Gizmos.color = Color.green; // Verde es el nodo inicial
                v = graph.GetNearestVertex(srcObj.transform.position);
                Gizmos.DrawSphere(v.transform.position, pathNodeRadius);
            }
            if (!ReferenceEquals(dstObj, null))
            {
                Gizmos.color = Color.red; // Rojo es el color del nodo de destino
                v = graph.GetNearestVertex(dstObj.transform.position);
                Gizmos.DrawSphere(v.transform.position, pathNodeRadius);
            }
            int i;
            Gizmos.color = pathColor;
            for (i = 0; i < path.Count; i++)
            {
                v = path[i];
                Gizmos.DrawSphere(v.transform.position, pathNodeRadius);
            }
        }

        // Mostrar el camino calculado
        public void ShowPathVertices(List<Vertex> path, Color color)
        {
            int i;
            for (i = 0; i < path.Count; i++)
            {
                Vertex v = path[i];
                Renderer r = v.GetComponent<Renderer>();
                if (ReferenceEquals(r, null))
                    continue;
                r.material.color = color;
            }
        }

        // Dibuja el hilo
        public virtual void DibujaHilo()
        {
            if (path == null)
                return;

            List<Vertex> reversedPath = GetReversedPath(path);

            hilo.positionCount = reversedPath.Count + 1;
            hilo.SetPosition(0, new Vector3(srcObj.transform.position.x, srcObj.transform.position.y + hiloOffset, srcObj.transform.position.z));

            for (int i = reversedPath.Count - 1; i >= 0; i--)
            {
                Vector3 vertexPos = new Vector3(reversedPath[i].transform.position.x, reversedPath[i].transform.position.y + hiloOffset, reversedPath[i].transform.position.z);
                hilo.SetPosition(reversedPath.Count - i, vertexPos);
            }
        }

        void UpdatePath(bool mode)
        {
            reUpdatePath = mode;
        }

        public virtual void ResetPath()
        {
            path = null;
        }

        public virtual void SetDestiny(GameObject gO)
        {
            dstObj = gO;
        }

        private List<Vertex> GetReversedPath(List<Vertex> originalPath)
        {
            List<Vertex> reversedPath = new List<Vertex>(originalPath);
            reversedPath.Reverse();
            return reversedPath;
        }

    }
}

