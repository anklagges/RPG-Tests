using UnityEngine;
using System.Collections;

public class Edificio : MonoBehaviour
{
    public EdificioData data;
    private Sprite m_imagenActual;
    private Transform transCiudad;

    void Awake()
    {
        transCiudad = transform.parent;
        m_imagenActual = this.GetComponent<SpriteRenderer>().sprite;
        data.Init(transform.position, transCiudad.position, m_imagenActual.bounds.size);
    }
}
