using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PosRutaActual
{
    public float m_tiempo;
    public int m_vecesEsperado;

    public PosRutaActual(float tiempo, int vecesEsperado)
    {
        m_tiempo = tiempo;
        m_vecesEsperado = vecesEsperado;
    }
}

public class PosRuta
{
    public float m_tiempo;
    public Vector2 m_posicion;

    public PosRuta(float tiempo, Vector2 pos)
    {
        m_tiempo = tiempo;
        m_posicion = pos;
    }
}

public class PathfinderNPC : MonoBehaviour
{
    public Dictionary<Vector2, PosRutaActual> rutaActual = new Dictionary<Vector2, PosRutaActual>();
    public PosRuta ultimaPosicion;

    //Componentes y Partes
    [HideInInspector]
    public NPC npc;
    [HideInInspector]
    public Pies pies;
    private Movimiento m_movimiento;

    //Auxiliares
    public string Nombre { get { return npc.nombre; } }
    private Ciudad m_ciudad;
    public List<Vector2> posOcupadas = new List<Vector2>();
    public List<Vector2> rutaOriginal = new List<Vector2>();
    private int contadorNPCs = 0;
    private Vector2? m_objetivoFinal;

    //Flags
    public bool enRuta;
    private bool considerarNPCs;


    private ControladorPathfinder m_controladorActual;

    public void Init()
    {
        npc = GetComponentInParent<NPC>();
        m_ciudad = npc.ciudad;
        pies = npc.pies;
        m_movimiento = npc.movimiento;
        AddPosActual();
    }

    void Update()
    {
        if (m_controladorActual != null)
        {
            m_controladorActual.Actualizar();
        }
    }

    public void AddPosActual()
    {
        posOcupadas.Add(GetPosActualGrilla());
    }

    public Vector2 GetPosActualGrilla()
    {
        return Utilidades.GetPosicionGrilla(transform.position, m_ciudad.transform);
    }

    public bool NpcCaminando()
    {
        return npc.estadoActual == EstadoNPC.Caminando;
    }

    public void ConsiderarNPCs(bool considerar)
    {
        //if (npc.nombre == "Pepe") Debug.Log("WA" + considerar);
        if (considerar) contadorNPCs++;
        else contadorNPCs--;
        considerarNPCs = contadorNPCs > 0;
    }

    public void SalirEdificio(Vector2 pos)
    {
        posOcupadas.Add(pos);
        SetRutaActual(pos);
    }

    public void EntrarEdificio()
    {
        considerarNPCs = false;
        contadorNPCs = 0;
    }

    public void BuscarNuevaRuta(NPC otroNPC)
    {
        if (m_movimiento.m_movActual != null)
        {
            m_movimiento.StopActual();
            RutaTo(rutaOriginal.Last(), false);
        }
    }

    /*private IEnumerator Reroute(PathfinderNPC otroNPC)
    {
        Debug.Log("REROUTE: " + npc.nombre);
        if (pies.moviendo) yield return new WaitWhile(() => pies.moviendo);
        float distancia = (Mathf.Abs(otroNPC.transform.position.x - transform.position.x) + Mathf.Abs(otroNPC.transform.position.y - transform.position.y));
        int espaciosExtras = Mathf.FloorToInt(distancia / GeneradorSuelo.sueloSize);
        if (espaciosExtras < 1) espaciosExtras = 1;
        int indice = rutaOriginal.FindIndex(x => x == posOcupadas[0]) + espaciosExtras;
        //Si el otro npc justo dejo de caminar para elegir el siguiente objetivo considera posOcupadas.count == 1;!!!! Provoca bug
        if (otroNPC.posOcupadas.Count > 1 && Vector2.Distance(otroNPC.posOcupadas[0], posOcupadas[0]) < Vector2.Distance(otroNPC.posOcupadas[1], posOcupadas[0]))
            indice += 10;//Temporal, deberia ser calculado en base a cuanto tiempo extra le tomara adelantarlo segun las velocidades maximas
        //Debug.Log(espaciosExtras);
        //Debug.Log(indice);
        //Debug.Log(rutaOriginal.Count);
        if (rutaOriginal.Count - indice > 0)
        {
            //Debug.Log(GetPosActualGrilla());
            RutaTo(rutaOriginal.GetRange(indice, rutaOriginal.Count - indice), false);
            yield return new WaitWhile(() => npc.estadoActual == EstadoNPC.Quieto);
            //Debug.Log("BACK ON TRACK!");
        }
        m_movimiento.SiguienteAccion();
    }*/

    public void RutaTo(Vector2 objetivo, bool esPosReal, bool esObjetivoFinal = false)
    {
        RutaTo(new List<Vector2>() { objetivo }, esPosReal, esObjetivoFinal);
    }

    //Obtiene la ruta al objetivo mas cercano
    //Define si dicha ruta llega a su destino
    private List<Vector2> GetRutaTo(List<Vector2> objetivos, bool esPosReal, out Vector2 objetivoFinal)
    {
        TreeNode nodoMasCercano;
        Vector2 posActual = GetPosActualGrilla();
        if (esPosReal) objetivos = Utilidades.GetPosicionesGrilla(objetivos, m_ciudad.transform);

        List<Vector2> posiciones = GetRuta(posActual, objetivos, considerarNPCs, out nodoMasCercano);
        bool esPosibleLlegar = true;
        if (posiciones == null && nodoMasCercano != null)
        {
            posiciones = PathFinder.TransformarRuta(nodoMasCercano.ObtenerRutaDesdeOrigen());
            esPosibleLlegar = false;
        }

        if (posiciones == null || (posiciones.Count == 2 && posiciones[0] == posiciones[1]))
        {
            objetivoFinal = GetObjetivoMasCercano(GetPosActualGrilla(), objetivos);
            return new List<Vector2>() { objetivoFinal };
        }
        else
        {
            if (esPosibleLlegar) objetivoFinal = posiciones.Last();
            else objetivoFinal = GetObjetivoMasCercano(posiciones.Last(), objetivos);
            return posiciones;
        }
    }

    public void RutaTo(List<Vector2> objetivos, bool esPosReal, bool esObjetivoFinal = false)
    {
        npc.estadoActual = EstadoNPC.Caminando;
        enRuta = true;
        Vector2 objetivoFinal;
        List<Vector2> ruta = GetRutaTo(objetivos, esPosReal, out objetivoFinal);
        if (esObjetivoFinal) m_objetivoFinal = objetivoFinal;

        if (ruta.Count == 1) Acercarse(ruta[0]);
        else
        {
            rutaOriginal = objetivos;
            m_controladorActual = new Avanzar(ruta);
            m_controladorActual.Init(npc, m_ciudad);
        }
    }


    private Vector2 GetObjetivoMasCercano(Vector2 posInicial, List<Vector2> objetivos)
    {
        float distancia;
        float menorDistancia = Vector2.Distance(objetivos[0], posInicial);
        Vector2 objetivoMasCercano = objetivos[0];
        for (int i = 1; i < objetivos.Count; i++)
        {
            distancia = Vector2.Distance(objetivos[i], posInicial);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                objetivoMasCercano = objetivos[i];
            }
        }
        return objetivoMasCercano;
    }

    public void Acercarse(Vector2 objetivo)
    {
        m_controladorActual = new Acercarse(objetivo);
        m_controladorActual.Init(npc, m_ciudad);
    }

    public void PedirPermiso(Vector2 objetivo)
    {
        npc.estadoActual = EstadoNPC.Quieto;
        m_controladorActual = new PedirPermiso(objetivo);
        m_controladorActual.Init(npc, m_ciudad);
    }

    public void DejarPasar(Vector2 posFinalOtro)
    {
        m_controladorActual = new DejarPasar(posFinalOtro);
        m_controladorActual.Init(npc, m_ciudad);
    }

    public void EsperarNPC(Vector2 objetivo)
    {
        npc.estadoActual = EstadoNPC.Esperando;
        m_controladorActual = new Esperar(objetivo);
        m_controladorActual.Init(npc, m_ciudad);
    }

    public bool EsPosibleMoverOcupar(Vector2 posInicial, Vector2 posFinal)
    {
        if (posInicial.x != posFinal.x && posInicial.y != posFinal.y) return false;
        if (posInicial.x == posFinal.x && Mathf.Abs(posInicial.y - posFinal.y) > 1) return false;
        if (posInicial.y == posFinal.y && Mathf.Abs(posInicial.x - posFinal.x) > 1) return false;
        return EsPosible(posInicial, posFinal, false);
    }

    public bool EsPosible(Vector2 posInicial, Vector2 posFinal, bool usarMargen = true)
    {
        return Suelo.EsPosible(posInicial, posFinal, m_ciudad, considerarNPCs, this, usarMargen);
    }

    public bool EsFuturaPosicion(Vector2 posicion)
    {
        return rutaActual.ContainsKey(posicion) && rutaActual[posicion].m_tiempo > Time.time;
    }

    public List<Vector2> GetRutaFiltrada()
    {
        List<Vector2> ruta = new List<Vector2>(rutaActual.Keys);
        return ruta.FindAll(x => EsFuturaPosicion(x));
    }


    public bool ComprobarObjetivo(Vector2 objetivo)
    {
        foreach (PathfinderNPC npc in m_ciudad.NPCs)
        {
            if (npc == this) continue;
            if (npc.posOcupadas.Contains(objetivo))
                return false;
        }
        return true;
    }

    /*private bool ComprobarObjetivos(List<Vector2> objetivos)
    {
        foreach (Vector2 objetivo in objetivos)
            if (ComprobarObjetivo(objetivo))
                return true;
        return false;
    }*/

    private List<Vector2> GetRuta(Vector2 posInicial, List<Vector2> objetivos, bool considerarNPCS, out TreeNode nodoMasCercano)
    {
        return PathFinder.GetVectoresRuta(posInicial, objetivos, m_ciudad, considerarNPCS, this, out nodoMasCercano);
    }

    public float TiempoEspera()
    {
        return 1 / (2 * CasillasPorSegundo());
    }

    public float CasillasPorSegundo()
    {
        return pies.GetVelocidadMaximaReal * 2;
    }

    private float Resistencia(int x, int y)
    {
        ESuelo tipo = (ESuelo)m_ciudad.PosicionesActuales[x, y];
        return Pies.m_resistenciasSuelo[tipo.ToString()];
    }

    public float TiempoPorCasilla(Vector2 pos)
    {
        return (1 + Resistencia((int)pos.x, (int)pos.y)) / CasillasPorSegundo();
    }

    public void MoverOcupar(Vector2 objetivo)
    {
        if (Vector2.Distance(objetivo, GetPosActualGrilla()) <= 1)
        {
            npc.estadoActual = EstadoNPC.Caminando;
            ClearRutaActual();
            Vector2 posActual = GetPosActualGrilla();
            if (objetivo != posActual)
                AddRutaActual(posActual, Time.time);
            AddRutaActual(objetivo, Time.time + TiempoPorCasilla(objetivo));
            pies.MoverOcupar(objetivo);
        }
        else
        {
            if (EsPosible(GetPosActualGrilla(), objetivo))
                RutaTo(objetivo, false);
            Debug.LogWarning("MUY LEJOS: " + objetivo);
        }
    }

    public void SetRutaActual(Vector2 objetivo)
    {
        ClearRutaActual();
        Vector2 posActual = GetPosActualGrilla();
        if (objetivo != posActual)
            AddRutaActual(posActual, Time.time);
        AddRutaActual(objetivo, Time.time + TiempoPorCasilla(objetivo));
    }

    private void AddRutaActual(Vector2 pos, float tiempo)
    {
        if (!rutaActual.ContainsKey(pos))
            rutaActual.Add(pos, new PosRutaActual(tiempo, 0));
        else rutaActual[pos].m_vecesEsperado++;
        ultimaPosicion = new PosRuta(tiempo, pos);
    }

    public void ClearRutaActual()
    {
        rutaActual.Clear();
        ultimaPosicion = null;
    }

    public void SetRutaActual(List<Vector2> posicionesGrilla)
    {
        ClearRutaActual();
        float tiempoActumulado = Time.time;
        for (int p = 0; p < posicionesGrilla.Count; p++)
        {
            tiempoActumulado += TiempoPorCasilla(posicionesGrilla[p]);
            AddRutaActual(posicionesGrilla[p], tiempoActumulado);
        }
    }

    public bool EsObjetivoFinal()
    {
        return m_objetivoFinal.HasValue && GetPosActualGrilla() == m_objetivoFinal;
    }

    public void FinRuta()
    {
        enRuta = false;
        m_objetivoFinal = null;
    }
}
