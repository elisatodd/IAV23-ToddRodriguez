namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    public class InvalidMapController : MonoBehaviour
    {
        [SerializeField] private GameObject gasolineError;
        [SerializeField] private GameObject unreachableError;

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

