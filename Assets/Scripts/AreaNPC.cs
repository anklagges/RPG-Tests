using UnityEngine;
using System;
using System.Collections;

public class AreaNPC : AreaInteractuable
{
    public string texto;
    
    protected override void Start()
    {
        base.Start();
        Transform npc = transform.parent;
        m_accion = () =>
        {
            GeneradorDialogo.Instance.InteraccionConNPC(texto, npc);
        };
    }
}
