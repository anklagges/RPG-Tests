using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CircleCollider2D))]
public class Pies : MonoBehaviour
{
    [HideInInspector]
    public bool moviendo;
    [HideInInspector]
    public float m_resistenciaActual;
    public static Dictionary<string, float> m_resistenciasSuelo = new Dictionary<string, float>();

    //Auxiliares
    private Ciudad ciudad;
    private NPC npc;
    private PathfinderNPC pathfinder;
    private CircleCollider2D col2D;
    private Vector3 posMovimiento;
    private Rigidbody2D rBody;
    private float ultimaDistancia;
    private float distanciaActual;
    private Vector3 direccion;
    private bool removerAlFinalizar;
    private List<string> m_suelosAdjacentes = new List<string>();

    public void Init()
    {
        npc = GetComponentInParent<NPC>();
        pathfinder = npc.pathfinder;
        col2D = GetComponent<CircleCollider2D>();
        rBody = npc.GetComponent<Rigidbody2D>();
        posMovimiento = transform.position;
        ciudad = npc.ciudad;
        LlenarResistencias();
    }

    void LlenarResistencias()
    {
        if (m_resistenciasSuelo.Count == 0)
        {
            m_resistenciasSuelo.Add(ESuelo.Edificio.ToString(), 0);
            m_resistenciasSuelo.Add(ESuelo.Personaje.ToString(), 0);
            m_resistenciasSuelo.Add(ESuelo.Camino.ToString(), 0);
            m_resistenciasSuelo.Add(ESuelo.Marmol.ToString(), -0.1f);
            m_resistenciasSuelo.Add(ESuelo.Tierra.ToString(), 0.1f);
            m_resistenciasSuelo.Add(ESuelo.Pasto.ToString(), 0.25f);
            m_resistenciasSuelo.Add(ESuelo.Arena.ToString(), 0.5f);
        }
    }

    public void Mover(Vector3 objetivo)
    {
        moviendo = true;
        ultimaDistancia = Mathf.Infinity;
        posMovimiento = objetivo;
        removerAlFinalizar = false;
    }

    public void MoverOcupar(Vector2 objetivo)
    {
        pathfinder.posOcupadas.Add(objetivo);
        Mover(Utilidades.GetPosicionReal(objetivo, ciudad.transform));
        removerAlFinalizar = true;
    }

    public void SetEnabledCol(bool enable)
    {
        col2D.enabled = enable;
        if (!enable) m_suelosAdjacentes.Clear();
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
                    pathfinder.posOcupadas.RemoveAt(0);
                moviendo = false;
            }
            else
            {
                direccion = posMovimiento - transform.position;
                rBody.velocity = direccion.normalized * npc.velocidadMaxima * (1 - m_resistenciaActual);
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
                m_velocidadMaximaReal = npc.velocidadMaxima / dragAux;
            }
            return m_velocidadMaximaReal;
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "Tile")
        {
            m_suelosAdjacentes.Add(col.name.Split('_')[0]);
            UpdateResistenciaSuelo();
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.tag == "Tile")
        {
            m_suelosAdjacentes.Remove(col.name.Split('_')[0]);
            UpdateResistenciaSuelo();
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        //Debug.Log("CHOQUE: " + npc.nombre + " -> " + col.gameObject.name);
        if (col.gameObject.tag == "NPC")
        {
            NPC otroNPC = col.gameObject.GetComponent<NPC>();
            if (otroNPC.ojos.DebeEsquivar(otroNPC, true))
            {
                pathfinder.BuscarNuevaRuta(otroNPC);
                Debug.Log("Nueva Ruta: " + npc.nombre);
            }
        }
    }

    private void UpdateResistenciaSuelo()
    {
        m_resistenciaActual = 0;
        for (int i = 0; i < m_suelosAdjacentes.Count; i++)
        {
            if (m_resistenciaActual < m_resistenciasSuelo[m_suelosAdjacentes[i]])
                m_resistenciaActual = m_resistenciasSuelo[m_suelosAdjacentes[i]];
        }
    }
}
