namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Clase usada para la muestra de la gasolina en pantalla
    /// </summary>
    public class HideMask : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] Vector2 initialSize;

        public void Hide()
        {
            rectTransform.sizeDelta = new Vector2(0.0f, 0.0f);
        }

        public void Show()
        {
            rectTransform.sizeDelta = initialSize;
        }
    }

}
