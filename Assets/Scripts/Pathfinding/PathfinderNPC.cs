using UnityEngine;
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

public class PathfinderNPC : MonoBehaviour
{
    public Dictionary<Vector2, PosRutaActual> rutaActual = new Dictionary<Vector2, PosRutaActual>();
    public Vector2? ultimaPosicion;
    public Vector2? penultimaPosicion;

    //Componentes y Partes
    private NPC npc;
    [HideInInspector]
    public Pies pies;
    private Movimiento m_movimiento;

    //Auxiliares
    public string Nombre { get { return npc.nombre; } }
    private Ciudad m_ciudad;
    private Coroutine corutinaAux;
    public List<Vector2> posOcupadas = new List<Vector2>();
    public List<Vector2> rutaOriginal = new List<Vector2>();
    private int contadorNPCs = 0;
    private Vector2? m_objetivoFinal;

    //Flags
    public bool enRuta;
    private bool rerouting;
    private bool considerarNPCs;

    public void Init()
    {
        npc = GetComponentInParent<NPC>();
        m_ciudad = npc.ciudad;
        pies = npc.pies;
        m_movimiento = npc.movimiento;
        AddPosActual();
    }

    private void AddPosActual()
    {
        posOcupadas.Add(GetPosActualGrilla());
    }

    private Vector2 GetPosActualGrilla()
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

    public void StopAux()
    {
        rerouting = false;
        if (corutinaAux != null) StopCoroutine(corutinaAux);
    }


    public void SalirEdificio(Vector2 pos)
    {
        posOcupadas.Add(pos);
        SetRutaActual(pos);
    }

    public void EntrarEdificio()
    {
        rerouting = false;
        considerarNPCs = false;
        contadorNPCs = 0;
    }

    public void BuscarNuevaRuta(NPC otroNPC)
    {
        if (!rerouting)
        {
            if (corutinaAux != null) StopCoroutine(corutinaAux);
            StartCoroutine(Reroute(otroNPC.pathfinder));
        }
    }

    private IEnumerator Reroute(PathfinderNPC otroNPC)
    {
        //rerouting = true;
        if (pies.moviendo) yield return new WaitWhile(() => pies.moviendo);
        if (m_objetivoFinal.HasValue)
            RutaTo(m_objetivoFinal.Value, false);
        /*float distancia = (Mathf.Abs(otroNPC.transform.position.x - transform.position.x) + Mathf.Abs(otroNPC.transform.position.y - transform.position.y));
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
        }*/
        /*RutaTo(rutaOriginal.Last(), false);
        yield return new WaitWhile(() => npc.estadoActual == EstadoNPC.Quieto);*/
        /*rerouting = false;
        RutaTo(m_objetivoFinal.Value, false);*/
        //m_movimiento.SiguienteAccion();
    }

    public void RutaTo(Vector2 objetivo, bool esPosReal, bool esObjetivoFinal = false)
    {
        RutaTo(new List<Vector2>() { objetivo }, esPosReal, esObjetivoFinal);
    }

    public void RutaTo(List<Vector2> objetivos, bool esPosReal, bool esObjetivoFinal = false)
    {
        if (corutinaAux != null) StopCoroutine(corutinaAux);
        npc.estadoActual = EstadoNPC.Caminando;
        enRuta = true;
        if (esPosReal) objetivos = Utilidades.GetPosicionesGrilla(objetivos, m_ciudad.transform);
        TreeNode nodoMasCercano;
        Vector2 posActual = GetPosActualGrilla();
        List<Vector2> posiciones = GetRuta(posActual, objetivos, considerarNPCs, out nodoMasCercano);
        if (posiciones == null && nodoMasCercano != null)
            posiciones = PathFinder.TransformarRuta(nodoMasCercano.ObtenerRutaDesdeOrigen());
        if (posiciones == null || (posiciones.Count == 2 && posiciones[0] == posiciones[1]))
        {
            Vector2 masCercano = GetObjetivoMasCercano(objetivos);
            if (esObjetivoFinal) m_objetivoFinal = masCercano;
            Acercarse(masCercano);
        }
        else
        {
            if (rerouting) rutaOriginal = objetivos;
            if (esObjetivoFinal)
                m_objetivoFinal = posiciones[posiciones.Count - 1];
            corutinaAux = StartCoroutine(GoRuta(posiciones));
        }
    }


    private Vector2 GetObjetivoMasCercano(List<Vector2> objetivos)
    {
        float distancia;
        float menorDistancia = Vector2.Distance(objetivos[0], GetPosActualGrilla());
        Vector2 objetivoMasCercano = objetivos[0];
        for (int i = 1; i < objetivos.Count; i++)
        {
            distancia = Vector2.Distance(objetivos[i], GetPosActualGrilla());
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                objetivoMasCercano = objetivos[i];
            }
        }
        return objetivoMasCercano;
    }

    private void Acercarse(Vector2 objetivo)
    {
        if (corutinaAux != null) StopCoroutine(corutinaAux);
        corutinaAux = StartCoroutine(CoAcercarse(objetivo));
    }

    IEnumerator CoAcercarse(Vector2 objetivo)
    {
        //Debug.Log("ACERCARSE: " + npc.nombre);
        /*if (npc.nombre == "Pepe")
        {
            Debug.Log(GetPosActualGrilla());
            Debug.Log(objetivo);
        }*/
        if (pies.moviendo)
            yield return new WaitWhile(() => pies.moviendo);
        ClearRutaActual();
        Vector2 objetivoSiguiente = GetObjetivoSiguientePosible(objetivo);
        while (objetivoSiguiente != GetPosActualGrilla())
        {
            MoverOcupar(objetivoSiguiente);
            yield return new WaitWhile(() => pies.moviendo);
            objetivoSiguiente = GetObjetivoSiguientePosible(objetivo);
            if (m_objetivoFinal.HasValue && GetPosActualGrilla() == m_objetivoFinal)
            {
                npc.estadoActual = EstadoNPC.Quieto;
                enRuta = false;
                m_objetivoFinal = null;
                yield break;
            }
        }
        corutinaAux = StartCoroutine(PedirPermiso(objetivo));
    }

    private bool EsPosibleMoverOcupar(Vector2 posInicial, Vector2 posFinal)
    {
        if (posInicial.x != posFinal.x && posInicial.y != posFinal.y) return false;
        if (posInicial.x == posFinal.x && Mathf.Abs(posInicial.y - posFinal.y) > 1) return false;
        if (posInicial.y == posFinal.y && Mathf.Abs(posInicial.x - posFinal.x) > 1) return false;
        return Suelo.EsPosible(posInicial, posFinal, m_ciudad, considerarNPCs, this, false);
    }

    private bool EsPosible(Vector2 posInicial, Vector2 posFinal)
    {
        return Suelo.EsPosible(posInicial, posFinal, m_ciudad, considerarNPCs, this, false);
    }

    IEnumerator PedirPermiso(Vector2 objetivo)
    {
        //Debug.Log("PEDIR PERMISO: " + npc.nombre);
        npc.estadoActual = EstadoNPC.Quieto;
        PathfinderNPC npcEsperado = null;
        Vector2 objetivoSiguiente = GetObjetivoSiguiente(objetivo);
        while (true)
        {
            npcEsperado = m_ciudad.NPCs.Find(x => x.posOcupadas.Contains(objetivoSiguiente));
            if (npcEsperado != null)
            {
                while (true)
                {
                    if (npcEsperado.posOcupadas.Contains(GetPosActualGrilla()) || !npcEsperado.NpcCaminando() || EsPosible(GetPosActualGrilla(), objetivoSiguiente))
                        break;
                    yield return null;
                }
                yield return new WaitWhile(() => pies.moviendo);
                if (GetPosActualGrilla().x - objetivoSiguiente.x == 0 || GetPosActualGrilla().y - objetivoSiguiente.y == 0)
                {
                    if (!npcEsperado.NpcCaminando())
                    {
                        //Debug.Log(npcEsperado.npc.nombre);
                        if (!EsFuturaPosicion(objetivo))
                            npcEsperado.DejarPasar(objetivo);
                        EsperarNPC(objetivo);
                        yield return new WaitWhile(() => npc.estadoActual == EstadoNPC.Esperando);
                    }
                    else if (EsPosibleMoverOcupar(GetPosActualGrilla(), objetivoSiguiente))
                        MoverOcupar(objetivoSiguiente);
                    else
                    {
                        DejarPasar(npcEsperado.ultimaPosicion.Value);
                        npcEsperado.EsperarNPC(npcEsperado.ultimaPosicion.Value);
                        yield return new WaitWhile(() => npcEsperado.npc.estadoActual == EstadoNPC.Esperando);
                    }

                    yield return new WaitWhile(() => pies.moviendo);
                }


                if (m_objetivoFinal.HasValue && GetPosActualGrilla() == m_objetivoFinal)
                {
                    //Debug.LogError(npc.nombre);
                    enRuta = false;
                    m_objetivoFinal = null;
                }
                else
                {
                    if (GetPosActualGrilla().x - objetivo.x != 0 && GetPosActualGrilla().y - objetivo.y != 0)
                    {
                        if (corutinaAux == null) RutaTo(objetivo, false);
                    }
                    else Acercarse(objetivo);
                }
                yield break;
            }
            else if (EsPosible(GetPosActualGrilla(), objetivo))
            {
                RutaTo(objetivo, false);
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private Vector2 GetObjetivoSiguientePosible(Vector2 objetivoFinal)
    {
        Vector2 posActual = GetPosActualGrilla();
        Vector2 distancia = objetivoFinal - posActual;
        Vector2 objetivoIntermedio;
        if (distancia.x != 0)
        {
            objetivoIntermedio = posActual + new Vector2(Mathf.Sign(distancia.x), 0);
            if (EsPosible(posActual, objetivoIntermedio))
                return objetivoIntermedio;
        }
        if (distancia.y != 0)
        {
            objetivoIntermedio = posActual + new Vector2(0, Mathf.Sign(distancia.y));
            if (EsPosible(posActual, objetivoIntermedio))
                return objetivoIntermedio;
        }
        return posActual;
    }

    private Vector2 GetObjetivoSiguiente(Vector2 objetivoFinal)
    {
        Vector2 posActual = GetPosActualGrilla();
        Vector2 distancia = objetivoFinal - posActual;
        if (distancia.x != 0)
            return posActual + new Vector2(Mathf.Sign(distancia.x), 0); ;
        if (distancia.y != 0)
            return posActual + new Vector2(0, Mathf.Sign(distancia.y));
        return posActual;
    }

    public void DejarPasar(Vector2 posFinalOtro)
    {
        StartCoroutine(CoDejarPasar(posFinalOtro));
    }

    IEnumerator CoDejarPasar(Vector2 posFinalOtro)
    {
        if (npc.estadoActual != EstadoNPC.Ocupado)
        {
            yield return new WaitWhile(() => pies.moviendo);
            Suelo sueloActual, sueloAux;
            if (posOcupadas.Count == 0) AddPosActual();
            sueloActual = new Suelo(posOcupadas[0], posOcupadas[0], m_ciudad, true, this);
            foreach (Data posible in sueloActual.GetPosibles())
            {
                sueloAux = (Suelo)posible;
                if (sueloAux.PosicionNPC != posFinalOtro)
                {
                    MoverOcupar(sueloAux.PosicionNPC);
                    yield return new WaitWhile(() => pies.moviendo);
                    npc.estadoActual = EstadoNPC.Quieto;
                    yield break;
                }
            }
        }
    }

    private bool EsFuturaPosicion(Vector2 posicion)
    {
        return rutaActual.ContainsKey(posicion) && rutaActual[posicion].m_tiempo > Time.time;
    }

    public List<Vector2> GetRutaFiltrada()
    {
        List<Vector2> ruta = new List<Vector2>(rutaActual.Keys);
        return ruta.FindAll(x => EsFuturaPosicion(x));
    }

    public void EsperarNPC(Vector2 objetivo)
    {
        if (corutinaAux != null) StopCoroutine(corutinaAux);
        corutinaAux = StartCoroutine(CoEsperarNPC(objetivo));
    }

    //Espera hasta que la ruta al objetivo sea valida o la siguiente posicion lo sea.
    IEnumerator CoEsperarNPC(Vector2 objetivo)
    {
        //Debug.Log("ESPERAR: " + npc.nombre);
        npc.estadoActual = EstadoNPC.Esperando;
        yield return new WaitWhile(() => pies.moviendo);
        Vector2 objetivoSiguiente = GetObjetivoSiguiente(objetivo);
        while (true)
        {
            Vector2 posActual = GetPosActualGrilla();
            if (EsPosible(posActual, objetivo))
            {
                RutaTo(objetivo, false);
                yield break;
            }
            else if (EsPosibleMoverOcupar(posActual, objetivoSiguiente))
            {
                MoverOcupar(objetivoSiguiente);
                yield return new WaitWhile(() => pies.moviendo);
                objetivoSiguiente = GetObjetivoSiguiente(objetivo);
                if (m_objetivoFinal.HasValue && GetPosActualGrilla() == m_objetivoFinal)
                {
                    npc.estadoActual = EstadoNPC.Quieto;
                    Debug.LogError(npc.nombre);
                    enRuta = false;
                    m_objetivoFinal = null;
                    yield break;
                }
            }
            yield return new WaitForSeconds(.1f);
        }
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

    IEnumerator GoRuta(List<Vector2> posicionesGrilla)
    {
        Vector3 posInicial = Utilidades.GetPosicionReal(posicionesGrilla[0], m_ciudad.transform);
        if (transform.position != posInicial)
        {
            //Debug.LogError(transform.position + " --> " + posInicial);
            pies.Mover(posInicial);
            yield return new WaitWhile(() => pies.moviendo);
        }
        if (!rerouting) rutaOriginal = posicionesGrilla;
        List<Vector2> nuevaRuta = null;
        int i = 1;
        TreeNode nodoMasCercano;
        List<Vector2> grillaRestante = new List<Vector2>(posicionesGrilla);
        while (i < posicionesGrilla.Count)
        {
            SetRutaActual(grillaRestante);
            //Esperar?
            if (posicionesGrilla[i] == posicionesGrilla[i - 1])
                yield return new WaitForSeconds(1 / pies.GetVelocidadMaximaReal);
            else
            {
                //Movimiento Normal
                if (!considerarNPCs || EsPosibleMoverOcupar(GetPosActualGrilla(), posicionesGrilla[i]))
                {
                    pies.MoverOcupar(posicionesGrilla[i]);
                    yield return new WaitWhile(() => pies.moviendo);
                }
                else
                {
                    nuevaRuta = GetRuta(posicionesGrilla[i - 1], rutaOriginal, considerarNPCs, out nodoMasCercano);
                    //if (nuevaRuta == null && nodoMasCercano != null)
                    //    nuevaRuta = PathFinder.TransformarRuta(nodoMasCercano.ObtenerRutaDesdeOrigen());
                    if (nuevaRuta != null && nuevaRuta.Count > 1 && posicionesGrilla[i] != nuevaRuta[1])
                    {
                        Debug.Log("Ruta Alternativa");
                        corutinaAux = StartCoroutine(GoRuta(nuevaRuta));
                    }
                    else
                    {
                        //if (npc.nombre == "Pepe") Debug.Log("WAT");
                        Acercarse(ultimaPosicion.Value);
                    }
                    yield break;
                }
            }
            //Comprobar un mejor camino
            /*if (rerouting && i < posicionesGrilla.Count - 1)
            {
                nuevaRuta = GetRuta(posicionesGrilla[i], rutaOriginal, considerarNPCs, out nodoMasCercano);
                if (nuevaRuta != null && nuevaRuta.Count > 1 && posicionesGrilla[i + 1] != nuevaRuta[1] && nuevaRuta.Count < posicionesGrilla.Count - i)
                {
                    //Debug.Log("Mejor Ruta!");
                    corutinaAux = StartCoroutine(GoRuta(nuevaRuta));
                    yield break;
                }
            }*/
            i++;
            if (grillaRestante.Count > 0)
                grillaRestante.RemoveAt(0);
        }
        //Debug.LogError(npc.nombre);
        npc.estadoActual = EstadoNPC.Quieto;
        ClearRutaActual();

        if (m_objetivoFinal.HasValue && GetPosActualGrilla() == m_objetivoFinal)
        {
            enRuta = false;
            m_objetivoFinal = null;
        }
        else if (m_objetivoFinal.HasValue) RutaTo(m_objetivoFinal.Value, false);
        else Debug.LogWarning("FIN RUTA, NO FINAL");
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

    private void MoverOcupar(Vector2 objetivo)
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

    private void SetRutaActual(Vector2 objetivo)
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
        penultimaPosicion = ultimaPosicion;
        ultimaPosicion = pos;
    }

    public void ClearRutaActual()
    {
        rutaActual.Clear();
        ultimaPosicion = null;
        penultimaPosicion = null;
    }

    private void SetRutaActual(List<Vector2> posicionesGrilla)
    {
        ClearRutaActual();
        float tiempoActumulado = Time.time;
        for (int p = 0; p < posicionesGrilla.Count; p++)
        {
            if (p == 0 && posicionesGrilla[p] == GetPosActualGrilla())
                AddRutaActual(posicionesGrilla[p], Time.time);
            else
            {
                tiempoActumulado += TiempoPorCasilla(posicionesGrilla[p]);
                AddRutaActual(posicionesGrilla[p], tiempoActumulado);
            }
        }
    }

    void OnDrawGizmos()
    {
        Vector3 extra = Vector3.zero;
        if (npc.nombre == "Francisca")
        {
            Gizmos.color = Color.red;
            extra = new Vector3(-0.05f, -0.05f);
        }
        else if (npc.nombre == "Paula")
        {
            Gizmos.color = Color.magenta;
            extra = new Vector3(0.05f, 0.05f);
        }
        else if (npc.nombre == "Roberto")
        {
            Gizmos.color = Color.grey;
            extra = new Vector3(-0.1f, -0.1f);
        }
        else if (npc.nombre == "Pepe")
        {
            Gizmos.color = Color.white;
            extra = new Vector3(0.1f, 0.1f);
        }
        /*if (rutaOriginal.Count > 0)
        {
            List<Vector3> rutaReal = new List<Vector3>();
            for (int i = 0; i < rutaOriginal.Count; i++)
                rutaReal.Add(Utilidades.GetPosicionReal(rutaOriginal[i], m_ciudad.transform));
            for (int i = 0; i < rutaReal.Count - 1; i++)
            {
                Gizmos.DrawLine(rutaReal[i] + extra, rutaReal[i + 1] + extra);
            }
        }*/
        List<Vector2> rutaFiltrada = GetRutaFiltrada();
        if (rutaFiltrada.Count > 0)
        {
            List<Vector3> rutaReal = new List<Vector3>();
            for (int i = 0; i < rutaFiltrada.Count; i++)
                rutaReal.Add(Utilidades.GetPosicionReal(rutaFiltrada[i], m_ciudad.transform));
            for (int i = 0; i < rutaReal.Count - 1; i++)
            {
                Gizmos.DrawLine(rutaReal[i] + extra, rutaReal[i + 1] + extra);
            }
        }
    }

    //Retorna la direccion a la ultima posicion en valores -1, 0 o 1
    public Vector2 DireccionObjetivo()
    {
        if (ultimaPosicion.HasValue)
        {
            Vector2 direccion = (ultimaPosicion.Value - GetPosActualGrilla()).normalized;
            return new Vector2(Mathf.Sign(direccion.x), Mathf.Sign(direccion.y));
        }
        return Vector2.zero;
    }
}
