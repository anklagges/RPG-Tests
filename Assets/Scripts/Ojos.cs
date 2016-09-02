using UnityEngine;
using System.Collections;

public class Ojos : MonoBehaviour
{
    private PathfinderNPC pathfinderNpc;

    void Start()
    {
        pathfinderNpc = transform.parent.GetComponent<PathfinderNPC>();
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "NPC")
        {
            pathfinderNpc.ConsiderarNPCs(true);
            PathfinderNPC npc = col.GetComponent<PathfinderNPC>();
            if (DebeEsquivar(npc))
            {
                pathfinderNpc.BuscarNuevaRuta(npc);
                //Debug.Log(pathfinderNpc.name + " debe esquivar a " + npc.name);
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.tag == "NPC")
            pathfinderNpc.ConsiderarNPCs(false);
    }

    private bool DebeEsquivar(PathfinderNPC npc)
    {
        if (npc.NpcEntrando() || npc.saliendoEdificio) return true;
        else if (pathfinderNpc.NpcEntrando() || pathfinderNpc.saliendoEdificio) return false;
        if (!pathfinderNpc.NpcCaminando()) return false;
        else if (!npc.NpcCaminando()) return true;
        else
        {
            if (pathfinderNpc.CasillasPorSegundo() > npc.CasillasPorSegundo()) return true;
            else if (pathfinderNpc.CasillasPorSegundo() < npc.CasillasPorSegundo()) return false;
            else return transform.position.x > npc.transform.position.x || (transform.position.x == npc.transform.position.x && transform.position.y > npc.transform.position.y);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        Debug.Log(col.gameObject.name);
    }
}
