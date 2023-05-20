
namespace IAV23.ElisaTodd
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UnityEngine.UIElements;
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance = null;

        [SerializeField] private GameObject player = null;

        // Textos UI
        Text fRText;
        Text heuristicText;
        Text invencibleText;
        Text label;
        Text label2;
        string mazeSize = "10x10";

        private int frameRate = 60;
        TrainGraph trainGraph;

        // Variables de timer de framerate
        int m_frameCounter = 0;
        float m_timeCounter = 0.0f;
        float m_lastFramerate = 0.0f;
        float m_refreshTime = 0.5f;

        private bool cameraPerspective = true;

        GameObject exitSlab = null;
        GameObject startSlab = null;

        GameObject exit = null;

        int numMinos = 1;

        private bool invencible = true;
        public bool Invencible
        {
            get { return invencible; }
        }
        public void CambiarInvencible()
        {
            invencible = !invencible;
            invencibleText.text = invencible.ToString();
        }

        private void Awake()
        {
            // Hacemos que el gestor del juego sea un Ejemplar Único
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        private void Start()
        {
            Application.targetFrameRate = frameRate;

            FindGO();
        }

        private void OnLevelWasLoaded(int level)
        {
            invencible = true;
            FindGO();
        }

        // Update is called once per frame
        void Update()
        {
            // Timer para mostrar el frameRate a intervalos
            if (m_timeCounter < m_refreshTime)
            {
                m_timeCounter += Time.deltaTime;
                m_frameCounter++;
            }
            else
            {
                m_lastFramerate = (float)m_frameCounter / m_timeCounter;
                m_frameCounter = 0;
                m_timeCounter = 0.0f;
            }

            // Texto con el framerate y 2 decimales
            if (fRText != null)
                fRText.text = (((int)(m_lastFramerate * 100 + .5) / 100.0)).ToString();


            //Input
            if (Input.GetKeyDown(KeyCode.R))
                RestartScene();
            if (Input.GetKeyDown(KeyCode.F))
                ChangeFrameRate();
            //if (Input.GetKeyDown(KeyCode.C))
            //    heuristicText.text = theseusGraph.ChangeHeuristic();
        }

        private void FindGO()
        {
            //if (SceneManager.GetActiveScene().name == "Menu") // Nombre de escena que habría que llevar a una constante
            //{
            //    label = GameObject.FindGameObjectWithTag("DDLabel").GetComponent<Text>();
            //    label2 = GameObject.FindGameObjectWithTag("MinoLabel").GetComponent<Text>();
            //}
            //else if (SceneManager.GetActiveScene().name == "Labyrinth") // Nombre de escena que habría que llevar a una constante
            //{
            //    fRText = GameObject.FindGameObjectWithTag("Framerate").GetComponent<Text>();
            //    heuristicText = GameObject.FindGameObjectWithTag("Heuristic").GetComponent<Text>();
            //    //theseusGraph = GameObject.FindGameObjectWithTag("TesterGraph").GetComponent<TheseusGraph>();
            //    exitSlab = GameObject.FindGameObjectWithTag("Exit");
            //    startSlab = GameObject.FindGameObjectWithTag("Start");
            //    player = GameObject.Find("Avatar");
            //}

            startSlab = GameObject.Find("StartSlab");
            exitSlab = GameObject.Find("ExitSlab");
            player = GameObject.Find("Avatar");
        }

        public GameObject GetPlayer()
        {
            if (player == null) player = GameObject.Find("Avatar");
            return player;
        }

        public void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }


        public void setNumMinos()
        {
            numMinos = int.Parse(label2.text);
        }

        public int getNumMinos()
        {
            return numMinos;
        }

        public void goToScene(string scene)
        {
            SceneManager.LoadScene(scene);
        }

        public GameObject GetExitNode()
        {
            return exit;
        }

        public void SetExit(int i, int j, float size)
        {
            exit = new GameObject(); exit.name = "Exit";
            exit.transform.position = new Vector3(i * size, 0, j * size);
            exitSlab.transform.position = new Vector3(i * size, 0.3f, j * size);
        }

        public void SetStart(int i, int j, float size)
        {
            player.transform.position = new Vector3(i * size, 0.2f, j * size);
            startSlab.transform.position = new Vector3(i * size, 0.2f, j * size);
        }

        private void ChangeFrameRate()
        {
            if (frameRate == 30)
            {
                frameRate = 60;
                Application.targetFrameRate = 60;
            }
            else
            {
                frameRate = 30;
                Application.targetFrameRate = 30;
            }
        }

        public void ChangeSize()
        {
            mazeSize = label.text;
        }
        public string getSize()
        {
            return mazeSize;
        }
    }
}