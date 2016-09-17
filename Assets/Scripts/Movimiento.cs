using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Movimiento : MonoBehaviour
{
    private NPC npc;
    private PathfinderNPC m_pathfinder;
    private BoxCollider2D col2D;
    private Pies pies;
    private Ojos ojos;

    //Auxiliares
    private Ciudad m_ciudad;
    public PatronMovimiento m_patronActual;

    public void Init()
    {
        npc = GetComponentInParent<NPC>();
        col2D = npc.col2D;
        m_ciudad = npc.ciudad;
        m_pathfinder = npc.pathfinder;
        pies = npc.pies;
        ojos = npc.ojos;
    }

    void Update()
    {
        if (m_patronActual != null) m_patronActual.Update();
    }

    private void PausarAnterior()
    {
        m_pathfinder.StopAux();
    }

    public void StartRutina(PosRutina[] rutina)
    {
        m_patronActual = new MovimientoRutina(rutina);
        m_patronActual.Init(npc);
    }

    public void UpdateEdificiosUtiles(List<EdificioData> edificios)
    {
        if (m_patronActual == null)
        {
            m_patronActual = new MovimientoEdificio(edificios);
            m_patronActual.Init(npc);
        }
        else
        {
            MovimientoEdificio mov = m_patronActual as MovimientoEdificio;
            mov.AddObjetivos(edificios);
        }
    }

    public void StartRandom()
    {
        m_patronActual = new MovimientoRandom();
        m_patronActual.Init(npc);
    }

    public void SiguienteAccion()
    {
        if (npc.estadoActual != EstadoNPC.Ocupado)
        {
            /*if (m_patronActual != null && m_patronActual is MovimientoEdificio && ((MovimientoEdificio)m_patronActual).ObjetivosTotales > 0)
                m_patronActual = new MovimientoEdificio();
            else*/
            if (npc.movimientosRandoms)
            {
                m_patronActual = new MovimientoRandom();
                m_patronActual.Init(npc);
            } if (m_patronActual != null)
                m_patronActual = null;
        }
        else npc.estadoActual = EstadoNPC.Quieto;
    }

    /// <summary>
    /// Retorna 1 si debe esquivar y -1 si no debe. Si no sabe retorna 0. Si no tiene patron retorna -1;
    /// </summary>
    public int DebeEsquivar(NPC otroNPC)
    {
        if (m_patronActual != null)
            return m_patronActual.DebeEsquivar(otroNPC);
        return -1;
    }
}

[Serializable]
public class PosRutina
{
    public float m_tiempoEspera;
    public Vector2 m_pos;
}

public abstract class PatronMovimiento
{
    protected NPC m_npc;
    protected PathfinderNPC m_pathfinder;
    protected Pies m_pies;
    protected Ciudad m_ciudad;

    public abstract void Update();

    /// <summary>
    /// Retorna 1 si debe esquivar y -1 si no debe. Si no sabe retorna 0.
    /// </summary>
    public abstract int DebeEsquivar(NPC otroNPC);

    public virtual void Init(NPC npc)
    {
        m_npc = npc;
        m_pathfinder = npc.pathfinder;
        m_pies = npc.pies;
        m_ciudad = npc.ciudad;
    }
}

public class MovimientoRandom : PatronMovimiento
{
    public override void Update()
    {
        if (m_pies.moviendo) return;
        Vector2 objetivoRandom = m_ciudad.GetCaminoRandom();
        m_pathfinder.RutaTo(objetivoRandom, false, true);
    }

    public override int DebeEsquivar(NPC otroNPC)
    {
        return 1;
    }
}

public class MovimientoEdificio : PatronMovimiento
{
    public int ObjetivosTotales { get { return m_objetivos.Count; } }

    //Las entradas de los edificios
    private List<EdificioData> m_objetivos = new List<EdificioData>();
    private EdificioData m_edificioObjetivo = new EdificioData();

    private EEstado m_estado;
    private enum EEstado
    {
        EnRuta,
        Posicionarse,
        Entrando,
        EnEdifio,
        Esperando,
        Saliendo
    }

    private float m_time;

    public MovimientoEdificio(List<EdificioData> edificios)
    {
        m_objetivos = new List<EdificioData>(edificios);
    }

    public override void Init(NPC npc)
    {
        base.Init(npc);
        GoNext();
    }

    public void AddObjetivos(List<EdificioData> edificios)
    {
        m_objetivos.AddRange(edificios);
    }

    private void GoNext()
    {
        List<Vector2> entradas = new List<Vector2>();
        m_objetivos.ForEach(x => entradas.Add(x.Entrada));
        m_pathfinder.RutaTo(entradas, true, true);
        m_estado = EEstado.EnRuta;
    }

    public override void Update()
    {
        switch (m_estado)
        {
            case EEstado.EnRuta:
                if (!m_pathfinder.enRuta)
                {
                    m_pathfinder.rutaOriginal.Clear();
                    m_npc.estadoActual = EstadoNPC.Ocupado;
                    m_edificioObjetivo = GetEdificioObjetivo();
                    if (m_edificioObjetivo.Entrada != m_npc.transform.position)
                    {
                        //Debug.LogError(transform.position + " --> " + edificioObjetivo.Entrada);
                        m_pies.Mover(m_edificioObjetivo.Entrada);
                    }
                    m_estado = EEstado.Posicionarse;
                }
                break;
            case EEstado.Posicionarse:
                if (!m_pies.moviendo)
                {
                    Entrar();
                    m_estado = EEstado.Entrando;
                }
                break;
            case EEstado.Entrando:
                if (!m_pies.moviendo)
                {
                    m_edificioObjetivo.entradaLibre = true;
                    m_pathfinder.EntrarEdificio();
                    m_time = 0;
                    m_estado = EEstado.EnEdifio;
                }
                break;
            case EEstado.EnEdifio:
                m_time += Time.deltaTime;
                if (m_time > Utilidades.HorasRealesToSecsJuego(m_edificioObjetivo.horasDuracion))
                {
                    m_npc.SatisfacerNecesidad(m_edificioObjetivo);
                    m_npc.ojos.Enable(true);
                    m_estado = EEstado.Esperando;
                }
                break;
            case EEstado.Esperando:
                Vector2 posSalida = Utilidades.GetPosicionGrilla(m_edificioObjetivo.Entrada, m_ciudad.transform);
                if (m_edificioObjetivo.entradaLibre && m_pathfinder.ComprobarObjetivo(posSalida))
                {
                    m_npc.estadoActual = EstadoNPC.Caminando;
                    Vector3 posSalidaReal = Utilidades.GetPosicionReal(posSalida, m_ciudad.transform);
                    m_pies.SetEnabledCol(true);
                    m_npc.col2D.enabled = true;
                    m_pathfinder.SalirEdificio(posSalida);
                    //Debug.LogError(transform.position + " --> " + posSalidaReal);
                    m_pies.Mover(posSalidaReal);
                    m_npc.StartCoroutine("CambiarAlpha", (1 / (2 * m_pies.GetVelocidadMaximaReal)));
                    m_edificioObjetivo.entradaLibre = false;
                    m_estado = EEstado.Saliendo;
                }
                break;
            case EEstado.Saliendo:
                if (!m_pies.moviendo)
                {
                    m_npc.estadoActual = EstadoNPC.Quieto;
                    m_edificioObjetivo.entradaLibre = true;
                    m_objetivos.Remove(m_edificioObjetivo);
                    m_pathfinder.ClearRutaActual();

                    if (m_objetivos.Count > 0) GoNext();
                    else m_npc.movimiento.SiguienteAccion();
                }
                break;
        }
    }

    private void Entrar()
    {
        //Debug.LogError(transform.position + " --> " + edificioObjetivo.Entrada + new Vector3(0, 0.5f));
        m_pies.Mover(m_edificioObjetivo.Entrada + new Vector3(0, 0.5f));
        m_npc.StartCoroutine("CambiarAlpha", (1 / (2 * m_pies.GetVelocidadMaximaReal)));
        m_pathfinder.posOcupadas.Clear();
        m_npc.ojos.Enable(false);
        m_npc.col2D.enabled = false;
        m_pies.SetEnabledCol(false);
        m_edificioObjetivo.entradaLibre = false;
    }

    public EdificioData GetEdificioObjetivo()
    {
        EdificioData edificioObjetivo = null;
        float distancia;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < m_objetivos.Count; i++)
        {
            distancia = Vector2.Distance(m_objetivos[i].Entrada, m_npc.transform.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                edificioObjetivo = m_objetivos[i];
            }
        }

        return edificioObjetivo;
    }

    public override int DebeEsquivar(NPC otroNPC)
    {
        if (otroNPC.movimiento.m_patronActual != null && otroNPC.movimiento.m_patronActual is MovimientoEdificio)
        {
            MovimientoEdificio movOtroNPC = otroNPC.movimiento.m_patronActual as MovimientoEdificio;
            if (movOtroNPC.GetEdificioObjetivo() == GetEdificioObjetivo())
            {
                if (m_estado == EEstado.Saliendo || movOtroNPC.m_estado == EEstado.Saliendo) return 1;
            }
            else
            {
                if (movOtroNPC.m_estado == EEstado.Entrando || movOtroNPC.m_estado == EEstado.Saliendo) return 1;
                else if (m_estado == EEstado.Entrando || m_estado == EEstado.Saliendo) return -1;
            }
        }
        return 0;
    }
}

public class MovimientoRutina : PatronMovimiento
{
    private EEstado m_estado;
    private enum EEstado
    {
        Moviendo,
        Esperando
    }

    private PosRutina[] m_rutina;
    private PosRutina m_actual;
    private int m_indicePosSiguiente = 0;
    private float m_time;

    public MovimientoRutina(PosRutina[] rutina)
    {
        m_rutina = rutina;
    }

    public override void Init(NPC npc)
    {
        base.Init(npc);
        GoNext();
    }

    public override void Update()
    {
        switch (m_estado)
        {
            case EEstado.Moviendo:
                if (!m_npc.pies.moviendo)
                {
                    m_time = 0;
                    m_estado = EEstado.Esperando;
                }
                break;
            case EEstado.Esperando:
                m_time += Time.deltaTime;
                if (m_time > m_actual.m_tiempoEspera)
                    GoNext();
                break;
        }

    }

    private void GoNext()
    {
        m_actual = m_rutina[m_indicePosSiguiente];
        m_npc.pathfinder.RutaTo(m_actual.m_pos, true, true);
        m_indicePosSiguiente++;
        if (m_indicePosSiguiente > m_rutina.Length)
            m_indicePosSiguiente = 0;
        m_estado = EEstado.Moviendo;
    }

    public override int DebeEsquivar(NPC otroNPC)
    {
        if (m_estado == EEstado.Esperando)
            return 1;
        return 0;
    }
}