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
    private Dictionary<NPC, bool> m_triggeredNPCs = new Dictionary<NPC, bool>();

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
            if (DebeEsquivar(otroNPC, false))
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

    public bool DebeEsquivar(NPC otroNPC, bool isCollision)
    {
        if (m_npc.estadoActual != EstadoNPC.Caminando) return false;
        else if (otroNPC.estadoActual != EstadoNPC.Caminando) return true;
        if (!isCollision && !RutasCruzadas(otroNPC)) return false;
        Debug.LogError(m_npc.nombre + " se cruzan con: " + otroNPC.nombre);
        int debeEsquivar = m_npc.movimiento.DebeEsquivar(otroNPC);
        if (debeEsquivar == 1) return true;
        else if (debeEsquivar == -1) return false;

        if (pathfinderNpc.CasillasPorSegundo() > otroNPC.pathfinder.CasillasPorSegundo()) return true;
        else if (pathfinderNpc.CasillasPorSegundo() < otroNPC.pathfinder.CasillasPorSegundo()) return false;
        else
        {
            Vector2 dirNPC = pathfinderNpc.DireccionObjetivo();
            Vector2 dirOtroNPC = otroNPC.pathfinder.DireccionObjetivo();
            //En direccion perpendicular?
            if ((dirNPC.x == 0 && dirOtroNPC.y == 0) || (dirNPC.y == 0 && dirOtroNPC.x == 0)) return false;
            //Sin direcciones opuestas?
            if ((dirNPC.x == dirOtroNPC.x || dirNPC.x == 0 || dirOtroNPC.x == 0) &&
                (dirNPC.y == dirOtroNPC.y || dirNPC.y == 0 || dirOtroNPC.y == 0))
            {
                if (dirNPC.x != 0 || dirOtroNPC.x != 0)
                {
                    Debug.LogError(m_npc.nombre + " MISMA DIRECCION X: " + dirNPC.x);
                    return Vector2.Distance(transform.position, transform.position + new Vector3(dirNPC.x, 0)) <
                        Vector2.Distance(otroNPC.transform.position, otroNPC.transform.position + new Vector3(dirNPC.x, 0));
                }
                else
                {
                    Debug.LogError(m_npc.nombre + " MISMA DIRECCION Y: " + dirNPC.x);
                    return Vector2.Distance(transform.position, transform.position + new Vector3(0, dirNPC.y)) <
                        Vector2.Distance(otroNPC.transform.position, otroNPC.transform.position + new Vector3(0, dirNPC.y));
                }
            }
            else return transform.position.x > otroNPC.transform.position.x || (transform.position.x == otroNPC.transform.position.x && transform.position.y > otroNPC.transform.position.y);
        }
    }

    //Verifica si las rutas se cruzan, si el otro npc ya lo verifico entonces obtiene su valor.
    private bool RutasCruzadas(NPC otroNPC)
    {
        bool rutasCruzadas;
        if (m_triggeredNPCs.ContainsKey(otroNPC))
        {
            rutasCruzadas = m_triggeredNPCs[otroNPC];
            m_triggeredNPCs.Remove(otroNPC);
        }
        else
        {
            rutasCruzadas = Suelo.RutasCruzadas(pathfinderNpc.GetRutaFiltrada(), otroNPC.pathfinder.GetRutaFiltrada(), m_npc.ciudad, pathfinderNpc, otroNPC.pathfinder);
            otroNPC.ojos.m_triggeredNPCs.Add(m_npc, rutasCruzadas);
        }
        return rutasCruzadas;
    }
}
