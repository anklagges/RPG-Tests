using UnityEngine;
using System.Collections;

public class Edificio : MonoBehaviour
{
    //Propiedades
    public Vector3 entrada;
    public string necesidad;
    public ESuelo suelo;
    public int costo;
    public float horasSatisfaccion;
    public float horasDuracion;
    public bool entradaLibre;

    //Auxiliares;
    private Sprite imagenActual;
    private Transform ciudad;

    void Awake()
    {
        entradaLibre = true;
        ciudad = transform.parent;
        imagenActual = this.GetComponent<SpriteRenderer>().sprite;
    }

    public Vector3 Posicion
    {
        get
        {
            return transform.position;
        }
        set
        {
            transform.position = value;
        }
    }

    /// <summary>
    /// Devuelve la posicion relativa a la ciudad
    /// </summary>
    /// <returns></returns>
    public Vector3 PosicionRelativa
    {
        get
        {
            return transform.position - ciudad.position;
        }
    }

    private int m_ancho = -1;
    public int Ancho
    {
        get
        {
            if (m_ancho == -1)
                m_ancho = Mathf.RoundToInt(imagenActual.bounds.size.x);
            return m_ancho;
        }
    }

    private int m_alto = -1;
    public int Alto
    {
        get
        {
            if (m_alto == -1)
                m_alto = Mathf.RoundToInt(imagenActual.bounds.size.y);
            return m_alto;
        }
    }

    public Vector3 Entrada
    {
        get
        {
            return entrada + Posicion;
        }
        set
        {
            entrada = value;
        }
    }
}
