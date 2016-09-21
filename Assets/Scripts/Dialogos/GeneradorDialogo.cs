using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Burbuja
{
    public GameObject m_objeto;
    public Transform m_npc;
    public Burbuja(GameObject objeto, Transform npc)
    {
        m_objeto = objeto;
        m_npc = npc;
    }
}

public class GeneradorDialogo : MonoSingleton<GeneradorDialogo>
{
    public Text m_burbujaDialogo;
    private Transform m_canvas;
    private Burbuja m_actual;
    private Player m_player;

    void Start()
    {
        m_player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        m_canvas = GameObject.FindGameObjectWithTag("World UI").transform;
    }

    public void InteraccionConNPC(string texto, Transform npc)
    {
        if (HablandoCon(npc))
            Cancelar();
        else Generar(texto, npc);
    }

    private bool HablandoCon(Transform npc)
    {
        if (m_actual != null)
        {
            return m_actual.m_npc == npc;
        }
        return false;
    }

    private void Generar(string texto, Transform npc)
    {
        Text burbuja = Instantiate(m_burbujaDialogo);
        burbuja.text = texto;
        burbuja.transform.SetParent(m_canvas, false);
        burbuja.transform.position = npc.position + Vector3.up * 1.5f;
        m_actual = new Burbuja(burbuja.gameObject, npc);
        m_player.OnMoved += CancelarNPC;
    }

    private void CancelarNPC()
    {
        m_player.OnMoved -= Cancelar;
        Cancelar();
    }

    private void Cancelar()
    {
        if (m_actual != null)
        {
            Destroy(m_actual.m_objeto);
            m_actual = null;
        }
    }
}
