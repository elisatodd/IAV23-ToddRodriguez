namespace IAV23.ElisaTodd
{
    using UnityEngine;

    /// <summary>
    /// Controla la muestra de errores cuando se busca la soluci�n para un mapa no v�lido
    /// </summary>
    public class InvalidMapController : MonoBehaviour
    {
        // Variables para los game objects de cada tipo de error distinto
        [SerializeField] private GameObject gasolineError;
        [SerializeField] private GameObject unreachableError;

        // Subscripci�n a eventos del grafo, lanzados cuando analiza un mapa y encuentra errores
        private void OnEnable()
        {
            Graph.InvalidMap += ShowText;
        }
        private void OnDisable()
        {
            Graph.InvalidMap -= ShowText;
        }

        private void ShowText(bool unreachable)
        {
            unreachableError.SetActive(unreachable);
            gasolineError.SetActive(!unreachable);
        }
    }
}

