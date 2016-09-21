using UnityEngine;
using System;
using System.Collections;

public enum ECardinalidad
{
    Norte,
    Sur,
    Este,
    Oeste,
    Horizontal,
    Vertical,
    Full
}

public class AreaInteractuable : MonoBehaviour
{
    public ECardinalidad m_cardinalidad;
    private Vector2[] m_direcciones;
    protected Action m_accion;

    protected virtual void Start()
    {
        switch (m_cardinalidad)
        {
            case ECardinalidad.Norte: m_direcciones = new Vector2[] { Vector2.up };
                break;
            case ECardinalidad.Sur: m_direcciones = new Vector2[] { Vector2.down };
                break;
            case ECardinalidad.Este: m_direcciones = new Vector2[] { Vector2.right };
                break;
            case ECardinalidad.Oeste: m_direcciones = new Vector2[] { Vector2.left };
                break;
            case ECardinalidad.Horizontal: m_direcciones = new Vector2[] { Vector2.right, Vector2.left };
                break;
            case ECardinalidad.Vertical: m_direcciones = new Vector2[] { Vector2.up, Vector2.down };
                break;
            default:
                break;
        }
    }

    protected void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "Player")
        {
            Player player = col.GetComponent<Player>();
            player.m_areasAdjacentes.Add(this);
        }
    }

    protected void OnTriggerExit2D(Collider2D col)
    {
        if (col.tag == "Player")
        {
            Player player = col.GetComponent<Player>();
            player.m_areasAdjacentes.Remove(this);
        }
    }

    public void Accion(Vector3 direccionPlayer)
    {
        bool direccionAceptable = m_cardinalidad == ECardinalidad.Full;
        for (int i = 0; i < m_direcciones.Length; i++)
        {
            //Considera las posiciones opuestas y las diagonales (-1 y ~0.7)
            if (Vector2.Dot(direccionPlayer, m_direcciones[i]) <= -0.7f)
                direccionAceptable = true;
        }
        if (direccionAceptable)
        {
            if (m_accion != null)
            {
                m_accion();
            }
        }
    }
}
