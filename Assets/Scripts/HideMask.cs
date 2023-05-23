namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

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
