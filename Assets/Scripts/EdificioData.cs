using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class EdificioData
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
    private Vector3 m_imagenSize;
    private Vector3 m_posCiudad;
    private Vector3 m_posicion;
    public Vector3 Posicion { get { return m_posicion; } }

    public void Init(Vector3 posicion, Vector3 posCiudad, Vector3 imagenSize)
    {
        m_posicion = posicion;
        m_posCiudad = posCiudad;
        m_imagenSize = imagenSize;
        entradaLibre = true;
    }

    /// <summary>
    /// Devuelve la posicion relativa a la ciudad
    /// </summary>
    /// <returns></returns>
    public Vector3 PosicionRelativa { get { return m_posicion - m_posCiudad; } }

    private int m_ancho = -1;
    public int Ancho
    {
        get
        {
            if (m_ancho == -1)
                m_ancho = Mathf.RoundToInt(m_imagenSize.x);
            return m_ancho;
        }
    }

    private int m_alto = -1;
    public int Alto
    {
        get
        {
            if (m_alto == -1)
                m_alto = Mathf.RoundToInt(m_imagenSize.y);
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