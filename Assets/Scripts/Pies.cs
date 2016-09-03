﻿using UnityEngine;
using System.Collections;

public class Pies : MonoBehaviour
{
    public float velocidadMaxima;
    public bool moviendo;

    //Auxiliares
    private Transform transCiudad;
    private PathfinderNPC pathfinder;
    private BoxCollider2D boxCollider2D;
    private Vector3 posMovimiento;
    private Rigidbody2D rBody;
    private float ultimaDistancia;
    private float distanciaActual;
    private Vector3 velocidadMovimiento;
    private Vector3 direccion;
    private bool removerAlFinalizar;

    void Start()
    {
        pathfinder = transform.parent.GetComponent<PathfinderNPC>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        rBody = transform.parent.GetComponent<Rigidbody2D>();
        posMovimiento = transform.position;
        transCiudad = transform.parent.parent.parent;
    }

    public void Mover(Vector3 objetivo)
    {
        //rBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        moviendo = true;
        ultimaDistancia = Mathf.Infinity;
        posMovimiento = objetivo;
        removerAlFinalizar = false;
    }

    public void MoverOcupar(Vector2 objetivo)
    {
        pathfinder.posOcupadas.Add(objetivo);
        Mover(Utilidades.GetPosicionReal(objetivo, transCiudad));
        removerAlFinalizar = true;
    }

    public void SetEnabledCol(bool enable)
    {
        boxCollider2D.enabled = enable;
    }

    void FixedUpdate()
    {
        if (moviendo)
        {
            distanciaActual = Vector3.Distance(transform.position, posMovimiento);
            if (distanciaActual >= ultimaDistancia || distanciaActual < 0.05f)
            {
                rBody.MovePosition(posMovimiento);
                rBody.velocity = Vector3.zero;
                if (removerAlFinalizar)
                {
                    //pathfinder.rutaActual.Remove(pathfinder.posOcupadas[0]);
                    pathfinder.posOcupadas.RemoveAt(0);
                }
                moviendo = false;
            }
            else
            {
                direccion = posMovimiento - transform.position;
                rBody.velocity = direccion.normalized * velocidadMaxima;
            }
            ultimaDistancia = distanciaActual;
        }
    }



    //Velocidad Real = velocidadMaxima * Drag
    //3.33 = 4 * x => x = 0.83 (10 Drag)
    //1.33 = 4 * x => x = 0.33 (100 Drag)

    //Time.fixedDeltaTime = 0.02
    //Multiplicador = 1/(1+10*0.02) = 0.83
    //Multiplicador = 1/(1+100*0.02) = 0.33
    private float m_velocidadMaximaReal = -1;
    public float GetVelocidadMaximaReal
    {
        get
        {
            if (m_velocidadMaximaReal == -1)
            {
                float dragAux = 1 + rBody.drag * Time.fixedDeltaTime;
                m_velocidadMaximaReal = velocidadMaxima / dragAux;
            }
            return m_velocidadMaximaReal;
        }
    }
}