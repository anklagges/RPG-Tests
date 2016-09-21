using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GeneradorDialogo : MonoBehaviour
{
    public Text m_burbujaDialogo;

    public void Generar(string texto)
    {
        Text burbuja = Instantiate(m_burbujaDialogo);
        burbuja.text = texto;
    }
}
