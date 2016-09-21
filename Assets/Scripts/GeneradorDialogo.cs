using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GeneradorDialogo : MonoSingleton<GeneradorDialogo>
{
    public Text m_burbujaDialogo;
    private Transform m_canvas;

    void Start()
    {
        m_canvas = GameObject.FindGameObjectWithTag("World UI").transform;
    }

    public void Generar(string texto, Transform npc)
    {
        Text burbuja = Instantiate(m_burbujaDialogo);
        burbuja.text = texto;
        burbuja.transform.SetParent(m_canvas, false);
        burbuja.transform.position = npc.position + Vector3.up * 1.5f;
    }
}
