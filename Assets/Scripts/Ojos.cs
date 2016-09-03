using UnityEngine;
using System.Collections;

public class Ojos : MonoBehaviour
{
    private NPC m_npc;
    private PathfinderNPC pathfinderNpc;

    public void Init()
    {
        m_npc = GetComponentInParent<NPC>();
        pathfinderNpc = m_npc.pathfinder;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "NPC")
        {
            pathfinderNpc.ConsiderarNPCs(true);
            NPC npc = col.GetComponent<NPC>();
            if (DebeEsquivar(npc))
            {
                pathfinderNpc.BuscarNuevaRuta(npc);
                //Debug.Log(m_npc.name + " debe esquivar a " + npc.name);
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.tag == "NPC")
            pathfinderNpc.ConsiderarNPCs(false);
    }

    private bool DebeEsquivar(NPC npc)
    {
        if (npc.estadoActual == EstadoNPC.Entrando || npc.movimiento.saliendoEdificio) return true;
        else if (pathfinderNpc.NpcEntrando() || m_npc.movimiento.saliendoEdificio) return false;
        if (!pathfinderNpc.NpcCaminando()) return false;
        else if (npc.estadoActual != EstadoNPC.Caminando) return true;
        else
        {
            if (pathfinderNpc.CasillasPorSegundo() > npc.pathfinder.CasillasPorSegundo()) return true;
            else if (pathfinderNpc.CasillasPorSegundo() < npc.pathfinder.CasillasPorSegundo()) return false;
            else return transform.position.x > npc.transform.position.x || (transform.position.x == npc.transform.position.x && transform.position.y > npc.transform.position.y);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        Debug.Log(col.gameObject.name);
    }
}
