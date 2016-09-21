using UnityEngine;
using System;
using System.Collections;

public class AreaNPC : AreaInteractuable
{
    public string texto;

    protected override void Start()
    {
        base.Start();
        m_accion = () =>
        {
            GeneradorDialogo generador = GameObject.Find("Generador").GetComponent<GeneradorDialogo>();
            generador.Generar(texto);//to.do entregar transform del npc para posicionar y mejorar FIND CON MONOSINGLETON
        };
    }
}
