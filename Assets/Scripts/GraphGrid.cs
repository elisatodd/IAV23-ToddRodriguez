namespace IAV23.ElisaTodd
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;

    public class GraphGrid : Graph
    {
        const int MAX_TRIES = 1000;

        // casillas de comienzo y fin
        public GameObject endPrefab;
        public GameObject startPrefab;

        // estaciones de tren
        public GameObject verticalStationPrefab;
        public GameObject horizontalStationPrefab;

        // obstáculos
        public GameObject gasPrefab;
        public GameObject rockPrefab;
        public GameObject treePrefab;
        public GameObject housePrefab;

        public string mapsDir = "Maps"; // Directorio por defecto
        [SerializeField] string mapName = "Train1.map"; // Fichero por defecto

        public bool get8Vicinity = false;
        public float cellSize = 1f;
        [Range(0, Mathf.Infinity)]
        public float defaultCost = 1f;
        [Range(0, Mathf.Infinity)]
        public float maximumCost = Mathf.Infinity;

        GameObject[] vertexObjs;

        // De qué tipo puede ser cada casilla
        public enum CellType
        {
            Ground,
            Gasoline,
            Rock,
            Tree,
            House,
            VerticalStation,
            HorizontalStation,
            Start,
            Exit,
            Empty,
            Wall
        }

        public int GridToId(int x, int y)
        {
            return Math.Max(numRows, numCols) * y + x;
        }

        public Vector2 IdToGrid(int id)
        {
            Vector2 location = Vector2.zero;
            location.y = Mathf.Floor(id / numCols);
            location.x = Mathf.Floor(id % numCols);
            return location;
        }

        // Método para cargar y crear el mapa desde fichero
        private void LoadMap(string filename)
        {
            string path;

            path = Application.streamingAssetsPath + "/" + mapsDir + "/" + filename;

            try
            {
                StreamReader strmRdr = new StreamReader(path);
                using (strmRdr)
                {
                    int j = 0, i = 0, id = 0;
                    string line;

                    Vector3 position = Vector3.zero;
                    Vector3 scale = Vector3.zero;

                    line = strmRdr.ReadLine();
                    line = strmRdr.ReadLine();
                    numRows = int.Parse(line.Split(' ')[1]);
                    line = strmRdr.ReadLine();
                    numCols = int.Parse(line.Split(' ')[1]);
                    line = strmRdr.ReadLine();

                    // vertices guarda cada Vértice del mapa
                    vertices = new List<Vertex>(numRows * numCols);
                    neighbourVertex = new List<List<Vertex>>(numRows * numCols);
                    // vertexObjs guarda el gameObject que hay en cada vértice
                    vertexObjs = new GameObject[numRows * numCols];
                    // para leer los tipos de elementos en cada posición
                    CellType[,] readMap = new CellType[numRows, numCols];
                    // cada vértice tiene coste distinto dependiendo del tipo
                    costsVertices = new float[numRows, numCols];

                    mapVertices = new bool[numRows, numCols];

                    // Leer mapa
                    for (i = 0; i < numRows; i++)
                    {
                        line = strmRdr.ReadLine();
                        for (j = 0; j < numCols; j++)
                        {
                            bool isGround = true;

                            if (line[j] == 'E')
                            { // celda de salida
                                GameManager.instance.SetExit(j, i, cellSize);
                                readMap[i, j] = CellType.Exit;
                            }
                            else if (line[j] == 'S')
                            { // celda de entrada
                                GameManager.instance.SetStart(j, i, cellSize);
                                readMap[i, j] = CellType.Ground;
                            }
                            else if (line[j] == 'G')
                            {
                                readMap[i, j] = CellType.Ground;
                            }
                            else if (line[j] == 'r')
                            {
                                isGround = false;
                                readMap[i, j] = CellType.Rock;
                            }
                            else if (line[j] == 't')
                            {
                                isGround = false;
                                readMap[i, j] = CellType.Tree;
                            }
                            else if (line[j] == 'h')
                            {
                                isGround = false;
                                readMap[i, j] = CellType.House;
                            }
                            else if (line[j] == 'V')
                            {
                                readMap[i, j] = CellType.VerticalStation;
                            }
                            else if (line[j] == 'H')
                            {
                                readMap[i, j] = CellType.HorizontalStation;
                            }
                            else if (line[j] == 'g')
                            {
                                readMap[i, j] = CellType.Gasoline;
                            }
                            else
                            { // por defecto se pone suelo
                                readMap[i, j] = CellType.Ground;
                            }

                            mapVertices[i, j] = isGround;
                        }
                    }

                    // Generamos terreno
                    for (i = 0; i < numRows; i++)
                    {
                        for (j = 0; j < numCols; j++)
                        {
                            position.x = j * cellSize;
                            position.z = i * cellSize;

                            id = GridToId(j, i);

                            // se instancia un gameobject u otro en función de lo que se leyó
                            switch (readMap[i, j])
                            {
                                case CellType.Ground:
                                    vertexObjs[id] = Instantiate(vertexPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.Wall:
                                    vertexObjs[id] = Instantiate(vertexPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.Exit:
                                    vertexObjs[id] = Instantiate(endPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.Gasoline:
                                    vertexObjs[id] = Instantiate(gasPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.Rock:
                                    vertexObjs[id] = Instantiate(rockPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.Tree:
                                    vertexObjs[id] = Instantiate(treePrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.House:
                                    vertexObjs[id] = Instantiate(housePrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.VerticalStation:
                                    vertexObjs[id] = Instantiate(verticalStationPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                case CellType.HorizontalStation:
                                    vertexObjs[id] = Instantiate(horizontalStationPrefab, position, Quaternion.identity, this.gameObject.transform) as GameObject;
                                    break;
                                default:
                                    break;
                            }

                            if (vertexObjs[id] != null)
                            {
                                vertexObjs[id].name = vertexObjs[id].name.Replace("(Clone)", id.ToString());
                                Vertex v = vertexObjs[id].AddComponent<Vertex>();
                                v.id = id;
                                vertices.Add(v);
                                neighbourVertex.Add(new List<Vertex>());

                                vertexObjs[id].transform.localScale *= cellSize;
                            }
                        }
                    }

                    // Leemos vecinos
                    for (i = 0; i < numRows; i++)
                        for (j = 0; j < numCols; j++)
                            SetNeighbours(j, i);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public override void Load()
        {
            LoadMap(mapName);
        }

        protected void SetNeighbours(int x, int y, bool get8 = false)
        {
            int col = x;
            int row = y;

            int i, j;
            int vertexId = GridToId(x, y);
            neighbourVertex[vertexId] = new List<Vertex>();
            Vector2[] pos = new Vector2[0];
            if (get8)
            {
                pos = new Vector2[8];
                int c = 0;
                for (i = row - 1; i <= row + 1; i++)
                {
                    for (j = col - 1; j <= col; j++)
                    {
                        pos[c] = new Vector2(j, i);
                        c++;
                    }
                }
            }
            else
            {
                pos = new Vector2[4];
                pos[0] = new Vector2(col, row - 1);
                pos[1] = new Vector2(col - 1, row);
                pos[2] = new Vector2(col + 1, row);
                pos[3] = new Vector2(col, row + 1);
            }

            foreach (Vector2 p in pos)
            {
                i = (int)p.y;
                j = (int)p.x;

                if (i < 0 || j < 0 ||
                    i >= numRows || j >= numCols ||
                    i == row && j == col ||
                    !mapVertices[i, j])
                    continue;

                int id = GridToId(j, i);
                neighbourVertex[vertexId].Add(vertices[id]);
                costsVertices[i, j] = defaultCost;
            }
        }

        public override Vertex GetNearestVertex(Vector3 position)
        {
            int col = (int)Math.Round(position.x / cellSize);
            int row = (int)Math.Round(position.z / cellSize);
            Vector2 p = new Vector2(col, row);
            List<Vector2> explored = new List<Vector2>();
            Queue<Vector2> queue = new Queue<Vector2>();
            queue.Enqueue(p);
            do
            {
                p = queue.Dequeue();
                col = (int)p.x;
                row = (int)p.y;
                int id = GridToId(col, row);
                if (mapVertices[row, col])
                    return vertices[id];

                if (!explored.Contains(p))
                {
                    explored.Add(p);
                    int i, j;
                    for (i = row - 1; i <= row + 1; i++)
                    {
                        for (j = col - 1; j <= col + 1; j++)
                        {
                            if (i < 0 || j < 0 ||
                                j >= numCols || i >= numRows ||
                                i == row && j == col)
                                continue;
                            queue.Enqueue(new Vector2(j, i));
                        }
                    }
                }
            } while (queue.Count != 0);
            return null;
        }

        public override GameObject GetRandomPos()
        {
            GameObject pos = null;
            int tries = 0;

            int i, j;
            do
            {
                i = UnityEngine.Random.Range(0, numRows);
                j = UnityEngine.Random.Range(0, numCols);
                tries++;
            } while (tries < MAX_TRIES && !mapVertices[i, j]);

            pos = vertexObjs[GridToId(j, i)];

            return pos;
        }

        public override void UpdateVertexCost(Vector3 position, float costMultiplier)
        {
            Vertex v = GetNearestVertex(position);

            Vector2 gridPos = IdToGrid(v.id);

            int x = (int)gridPos.y;
            int y = (int)gridPos.x;


            if (x > 0 && x < numRows - 1 && y > 0 && y < numCols - 1)
                costsVertices[x, y] = defaultCost * costMultiplier * costMultiplier;

            if (x > 0) costsVertices[x - 1, y] = defaultCost * costMultiplier;
            if (x < numRows - 1) costsVertices[x + 1, y] = defaultCost * costMultiplier;
            if (y > 0) costsVertices[x, y - 1] = defaultCost * costMultiplier;
            if (y < numCols - 1) costsVertices[x, y + 1] = defaultCost * costMultiplier;

        }

    }
}
