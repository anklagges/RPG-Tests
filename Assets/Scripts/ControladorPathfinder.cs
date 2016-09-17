using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

#region ControladorPathfinder
public class ControladorPathfinder
{
    protected NPC m_npc;
    protected Ciudad m_ciudad;
    protected PathfinderNPC m_pathfinder;

    public virtual void Actualizar() { }

    public void Init(NPC npc, Ciudad ciudad)
    {
        m_ciudad = ciudad;
        m_npc = npc;
        m_pathfinder = m_npc.pathfinder;
        Debug.Log(GetType() + ": " + m_npc.nombre);
    }

    protected Vector2 GetPosActual()
    {
        return m_pathfinder.GetPosActualGrilla();
    }

    protected Vector2 GetObjetivoSiguiente(Vector2 objetivoFinal)
    {
        Vector2 posActual = GetPosActual();
        Vector2 distancia = objetivoFinal - posActual;
        if (distancia.x != 0)
            return posActual + new Vector2(Mathf.Sign(distancia.x), 0); ;
        if (distancia.y != 0)
            return posActual + new Vector2(0, Mathf.Sign(distancia.y));
        return posActual;
    }
}
#endregion

#region Pedir Permiso
public class PedirPermiso : ControladorPathfinder
{
    private Vector2 m_objetivo;
    private Vector2 m_objetivoSiguiente;
    private PathfinderNPC m_npcEsperado;

    private EEstado m_estado;
    private enum EEstado
    {
        BuscarNPC,
        Prepararse,
        Actuar,
    }

    public PedirPermiso(Vector2 objetivo)
    {
        m_objetivo = objetivo;
        SetObjetivoSiguiente();
    }

    public override void Actualizar()
    {
        switch (m_estado)
        {
            case EEstado.BuscarNPC:
                m_npcEsperado = m_ciudad.NPCs.Find(x => x.posOcupadas.Contains(m_objetivoSiguiente));
                if (m_npcEsperado != null) m_estado = EEstado.Prepararse;
                break;
            case EEstado.Prepararse:
                if (!m_npc.pies.moviendo)
                {
                    if (m_npcEsperado.posOcupadas.Contains(GetPosActual()) || !m_npcEsperado.NpcCaminando() || m_pathfinder.EsPosible(GetPosActual(), m_objetivoSiguiente))
                        Actuar();
                }
                break;
            case EEstado.Actuar:
                if (!m_npc.pies.moviendo && m_npc.estadoActual != EstadoNPC.Esperando && m_npcEsperado.npc.estadoActual != EstadoNPC.Esperando)
                {
                    if (m_pathfinder.EsObjetivoFinal())
                    {
                        m_pathfinder.FinRuta();
                    }
                    else if (m_pathfinder.EsPosible(GetPosActual(), m_objetivo)) m_pathfinder.RutaTo(m_objetivo, false);
                    else
                    {
                        SetObjetivoSiguiente();
                        m_estado = EEstado.BuscarNPC;
                    }
                }
                break;
            default:
                break;
        }
    }

    private void SetObjetivoSiguiente()
    {
        m_objetivoSiguiente = GetObjetivoSiguiente(m_objetivo);
    }

    private void Actuar()
    {
        if (!m_npcEsperado.NpcCaminando())
        {
            //Debug.Log(npcEsperado.npc.nombre);
            if (!m_pathfinder.EsFuturaPosicion(m_objetivo))
                m_npcEsperado.DejarPasar(m_objetivo);
            m_pathfinder.EsperarNPC(m_objetivo);
        }
        else if (m_pathfinder.EsPosibleMoverOcupar(GetPosActual(), m_objetivoSiguiente))
            m_pathfinder.MoverOcupar(m_objetivoSiguiente);
        else
        {
            m_pathfinder.DejarPasar(m_npcEsperado.ultimaPosicion.Value);
            m_npcEsperado.EsperarNPC(m_npcEsperado.ultimaPosicion.Value);
        }
        m_estado = EEstado.Actuar;
    }
}
#endregion

#region Acercarse
public class Acercarse : ControladorPathfinder
{
    private List<Vector2> m_ruta;
    private Vector2 m_objetivo;

    private EEstado m_estado;
    private enum EEstado
    {
        Actuar,
        Validar,
    }

    public Acercarse(Vector2 objetivo)
    {
        m_objetivo = objetivo;
        m_ruta = GetRutaAcercarse(objetivo);
    }

    public override void Actualizar()
    {
        if (m_npc.pies.moviendo) return;
        switch (m_estado)
        {
            case EEstado.Actuar:
                Vector2 siguientPos = m_ruta[0];
                if (m_pathfinder.EsPosibleMoverOcupar(GetPosActual(), siguientPos))
                    m_pathfinder.MoverOcupar(siguientPos);
                else
                {
                    m_ruta = GetRutaAcercarse(m_objetivo);
                    if (m_ruta.Count == 0 || m_ruta[0] == siguientPos) break;
                    m_pathfinder.MoverOcupar(m_ruta[0]);
                }
                m_ruta.RemoveAt(0);
                m_estado = EEstado.Validar;
                break;
            case EEstado.Validar:
                if (m_pathfinder.EsObjetivoFinal())
                {
                    m_pathfinder.FinRuta();
                    m_npc.estadoActual = EstadoNPC.Quieto;
                }
                else if (m_ruta.Count == 0) m_pathfinder.PedirPermiso(m_objetivo);
                else m_estado = EEstado.Actuar;
                break;
            default:
                break;
        }

    }

    private List<Vector2> GetRutaAcercarse(Vector2 objetivo)
    {
        Debug.Log("GET RUTA ACERCARSE: " + m_npc.nombre);
        List<Vector2> ruta = new List<Vector2>();
        ruta.Add(GetPosActual());
        Vector2 objetivoSiguiente;
        Vector2 objetivoAnterior = GetPosActual();
        while (true)
        {
            objetivoSiguiente = GetObjetivoSiguientePosible(objetivoAnterior, objetivo);
            if (objetivoSiguiente != objetivoAnterior)
            {
                ruta.Add(objetivoSiguiente);
                objetivoAnterior = objetivoSiguiente;
            }
            else break;
        }
        m_pathfinder.SetRutaActual(ruta);
        ruta.RemoveAt(0);
        return ruta;
    }

    private Vector2 GetObjetivoSiguientePosible(Vector2 posInicial, Vector2 objetivoFinal)
    {
        Vector2 distancia = objetivoFinal - posInicial;
        Vector2 objetivoIntermedio;
        if (distancia.x != 0)
        {
            objetivoIntermedio = posInicial + new Vector2(Mathf.Sign(distancia.x), 0);
            if (m_pathfinder.EsPosible(posInicial, objetivoIntermedio))
                return objetivoIntermedio;
        }
        if (distancia.y != 0)
        {
            objetivoIntermedio = posInicial + new Vector2(0, Mathf.Sign(distancia.y));
            if (m_pathfinder.EsPosible(posInicial, objetivoIntermedio))
                return objetivoIntermedio;
        }
        return posInicial;
    }
}
#endregion

#region Dejar Pasar
public class DejarPasar : ControladorPathfinder
{
    private Vector2 m_posFinalOtro;

    public DejarPasar(Vector2 posFinalOtro)
    {
        m_posFinalOtro = posFinalOtro;
    }

    public override void Actualizar()
    {
        if (m_npc.pies.moviendo) return;
        if (m_npc.estadoActual != EstadoNPC.Ocupado)
        {
            Suelo sueloActual, sueloAux;
            if (m_pathfinder.posOcupadas.Count == 0) m_pathfinder.AddPosActual();
            sueloActual = new Suelo(m_pathfinder.posOcupadas[0], m_pathfinder.posOcupadas[0], m_ciudad, true, m_pathfinder);
            foreach (Data posible in sueloActual.GetPosibles())
            {
                sueloAux = (Suelo)posible;
                if (sueloAux.PosicionNPC != m_posFinalOtro)
                {
                    m_pathfinder.MoverOcupar(sueloAux.PosicionNPC);
                    //WaitPies
                    m_npc.estadoActual = EstadoNPC.Quieto;
                    return;
                }
            }
            //Pedir permiso para dejar pasar!
        }
    }
}
#endregion

#region Esperar
public class Esperar : ControladorPathfinder
{
    private Vector2 m_objetivo;
    private Vector2 m_objetivoSiguiente;

    private EEstado m_estado;
    private enum EEstado
    {
        Comprobar,
        Esperar
    }

    public Esperar(Vector2 objetivo)
    {
        m_objetivo = objetivo;
        m_objetivoSiguiente = GetObjetivoSiguiente(m_objetivo);
    }

    public override void Actualizar()
    {
        if (m_npc.pies.moviendo) return;
        switch (m_estado)
        {
            case EEstado.Comprobar:
                Vector2 posActual = GetPosActual();
                if (m_pathfinder.EsPosible(posActual, m_objetivo))
                    m_pathfinder.RutaTo(m_objetivo, false);
                else if (m_pathfinder.EsPosibleMoverOcupar(posActual, m_objetivoSiguiente))
                {
                    m_pathfinder.MoverOcupar(m_objetivoSiguiente);
                    m_estado = EEstado.Esperar;
                }
                break;
            case EEstado.Esperar:
                if (m_pathfinder.EsObjetivoFinal())
                {
                    m_npc.estadoActual = EstadoNPC.Quieto;
                    m_pathfinder.FinRuta();
                }
                else m_estado = EEstado.Comprobar;
                break;
            default:
                break;
        }
    }
}
#endregion

#region Avanzar
public class Avanzar : ControladorPathfinder
{
    private List<Vector2> m_ruta;
    private float m_tiempo;

    private EEstado m_estado;
    private enum EEstado
    {
        Avanzar,
        Esperar,
    }

    public Avanzar(List<Vector2> ruta)
    {
        m_ruta = ruta;
        Vector3 posInicial = Utilidades.GetPosicionReal(m_ruta[0], m_ciudad.transform);
        if (m_npc.transform.position != posInicial)
        {
            //Debug.LogError(transform.position + " --> " + posInicial);
            m_npc.pies.Mover(posInicial);
        }
        m_pathfinder.rutaOriginal = m_ruta;
        m_pathfinder.SetRutaActual(m_ruta);
    }

    public override void Actualizar()
    {
        switch (m_estado)
        {
            case EEstado.Avanzar:
                if (!m_npc.pies.moviendo) Siguiente();
                break;
            case EEstado.Esperar:
                m_tiempo += Time.deltaTime;
                if (m_tiempo > m_pathfinder.TiempoEspera())
                    Siguiente();
                break;
            default:
                break;
        }
    }

    private void Siguiente()
    {
        m_ruta.RemoveAt(0);
        if (m_ruta.Count > 0)
        {
            m_pathfinder.SetRutaActual(m_ruta);
            if (m_ruta[0] == m_ruta[1])
            {
                m_estado = EEstado.Esperar;
                m_tiempo = 0;
            }
            else
            {
                if (m_pathfinder.EsPosibleMoverOcupar(GetPosActual(), m_ruta[0]))
                {
                    m_npc.pies.MoverOcupar(m_ruta[0]);
                    m_estado = EEstado.Avanzar;
                }
                else m_pathfinder.RutaTo(m_ruta.Last(), false);
            }
        }
        else
        {
            if (m_pathfinder.EsObjetivoFinal())
            {
                m_pathfinder.FinRuta();
            }
            m_npc.estadoActual = EstadoNPC.Quieto;
            m_pathfinder.ClearRutaActual();
        }
    }
}
#endregion