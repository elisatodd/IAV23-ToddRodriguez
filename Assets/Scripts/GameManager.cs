
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

        [SerializeField] private int gasInitialLevel;
        [SerializeField] private int gasLevel;
        public int GasInitialLevel
        {
            get { return gasInitialLevel; }
        }
        public int GasLevel
        {
            get { return gasLevel; }
            set {  gasLevel = value; UpdateGasUI(); }
        }

        [Header("UI")]
        [SerializeField] private GameObject gasUI;
        [SerializeField] private VerticalLayoutGroup gasLayout;

        private List<HideMask> hideMasks;

        private int frameRate = 60;

        // Variables de timer de framerate
        int m_frameCounter = 0;
        float m_timeCounter = 0.0f;
        float m_lastFramerate = 0.0f;
        float m_refreshTime = 0.5f;


        GameObject exitSlab = null;
        GameObject startSlab = null;

        GameObject exit = null;

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

            BuildGasUI(); 

            GasLevel = GasInitialLevel;
        }

        private void BuildGasUI()
        {
            hideMasks = new List<HideMask>();
            for (int i = 0; i < gasInitialLevel; ++i)
            {
                hideMasks.Add(Instantiate(gasUI, gasLayout.transform).GetComponent<HideMask>());
            }
        }

        private void UpdateGasUI()
        {
            for (int i = 0; i < hideMasks.Count; ++i)
            {
                if (i < gasLevel)
                {
                    hideMasks[i]?.Show();
                }
                else
                {
                    hideMasks[i]?.Hide();
                }
            }
        }

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


            //Input
            if (Input.GetKeyDown(KeyCode.R))
                RestartScene();
            if (Input.GetKeyDown(KeyCode.F))
                ChangeFrameRate();
        }

        private void FindGO()
        {
            startSlab = GameObject.Find("StartSlab");
            exitSlab = GameObject.Find("ExitSlab");
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
    }
}