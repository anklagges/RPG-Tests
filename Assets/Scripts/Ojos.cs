using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
public class Ojos : MonoBehaviour
{
    private NPC m_npc;
    private PathfinderNPC pathfinderNpc;
    private List<NPC> m_npcsAdjacentes = new List<NPC>();
    private PolygonCollider2D m_col2D;

    public void Init()
    {
        m_col2D = GetComponent<PolygonCollider2D>();
        m_npc = GetComponentInParent<NPC>();
        pathfinderNpc = m_npc.pathfinder;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "NPC")
        {
            pathfinderNpc.ConsiderarNPCs(true);
            NPC otroNPC = col.GetComponent<NPC>();
            m_npcsAdjacentes.Add(otroNPC);
            if (DebeEsquivar(otroNPC))
            {
                pathfinderNpc.BuscarNuevaRuta(otroNPC);
                //Debug.Log(m_npc.name + " debe esquivar a " + npc.name);
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.tag == "NPC")
        {
            NPC otroNPC = col.GetComponent<NPC>();
            RemoverNPC(otroNPC);
        }
    }

    public void RemoverNPC(NPC otroNPC)
    {
        pathfinderNpc.ConsiderarNPCs(false);
        m_npcsAdjacentes.Remove(otroNPC);
    }

    public void Enable(bool enable)
    {
        m_col2D.enabled = enable;
        if (!enable)
        {
            for (int i = 0; i < m_npcsAdjacentes.Count; i++)
                m_npcsAdjacentes[i].ojos.RemoverNPC(m_npc);
        }
    }

    public bool DebeEsquivar(NPC otroNPC)
    {
        if (otroNPC.movimiento.GetEdificioObjetivo() == m_npc.movimiento.GetEdificioObjetivo())
        {
            if (m_npc.movimiento.saliendoEdificio || otroNPC.movimiento.saliendoEdificio) return true;
        }
        else
        {
            if (otroNPC.estadoActual == EstadoNPC.Entrando || otroNPC.movimiento.saliendoEdificio) return true;
            else if (m_npc.estadoActual == EstadoNPC.Entrando || m_npc.movimiento.saliendoEdificio) return false;
        }
        if (m_npc.estadoActual != EstadoNPC.Caminando) return false;
        else if (otroNPC.estadoActual != EstadoNPC.Caminando) return true;
        else
        {
            if (pathfinderNpc.CasillasPorSegundo() > otroNPC.pathfinder.CasillasPorSegundo()) return true;
            else if (pathfinderNpc.CasillasPorSegundo() < otroNPC.pathfinder.CasillasPorSegundo()) return false;
            else return transform.position.x > otroNPC.transform.position.x || (transform.position.x == otroNPC.transform.position.x && transform.position.y > otroNPC.transform.position.y);
        }
    }
}
