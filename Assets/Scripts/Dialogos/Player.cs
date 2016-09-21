using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    public Rigidbody2D rBody;
    public float m_velocidadMaxima;
    public List<AreaInteractuable> m_areasAdjacentes = new List<AreaInteractuable>();
    public Vector3 m_ultimaDireccion;
    public Action OnMoved;   

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(horizontal, vertical).normalized;
            rBody.velocity = dir * m_velocidadMaxima;
        if (horizontal != 0 || vertical != 0)
        {
            if (OnMoved != null)
            {
                OnMoved();
            }
            m_ultimaDireccion = dir;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_areasAdjacentes.ForEach(x => x.Accion(m_ultimaDireccion));
        }
    }
}
