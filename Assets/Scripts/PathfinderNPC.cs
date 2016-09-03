﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PathfinderNPC : MonoBehaviour
{
    public Dictionary<Vector2, float> rutaActual = new Dictionary<Vector2, float>();
    public int contadorEsperasRutaActual;

    //Componentes y Partes
    private NPC npc;
    private Pies pies;
    private Movimiento m_movimiento;

    //Auxiliares
    private Ciudad m_ciudad;
    private Coroutine corutinaAux;
    public List<Vector2> posOcupadas = new List<Vector2>();
    public List<Vector2> rutaOriginal = new List<Vector2>();
    private int contadorNPCs = 0;

    //Flags
    public bool enRuta;
    private bool rerouting;
    private bool considerarNPCs;
    private bool tienePreferencia = true;

    public Vector2 ultimaPosicion
    {
        get
        {
            if (rutaActual.Count > 0)
            {
                float tiempoMax = rutaActual.Values.Max();
                return rutaActual.First(x => x.Value == tiempoMax).Key;
            }
            return posOcupadas[0];
        }
    }

    public void Init()
    {
        npc = GetComponentInParent<NPC>();
        m_ciudad = npc.ciudad;
        pies = npc.pies;
        m_movimiento = npc.movimiento;
        posOcupadas.Add(GetPosActualGrilla());
    }

    void Update()
    {
        /*if (npc.nombre == "Pepe")
        {
            Debug.Log(tienePreferencia);
            Debug.Log(considerarNPCs);
        }*/
    }

    private Vector2 GetPosActualGrilla()
    {
        return Utilidades.GetPosicionGrilla(transform.position, m_ciudad.transform);
    }

    public bool NpcCaminando()
    {
        return npc.estadoActual == EstadoNPC.Caminando;
    }

    public bool NpcEntrando()
    {
        return npc.estadoActual == EstadoNPC.Entrando;
    }

    public void ConsiderarNPCs(bool considerar)
    {
        //if (npc.nombre == "Pepe") Debug.Log("WA" + considerar);
        if (considerar) contadorNPCs++;
        else contadorNPCs--;
        considerarNPCs = contadorNPCs > 0;
        if (considerarNPCs) tienePreferencia = true;
    }

    public void Stop()
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
        tienePreferencia = true;
        rerouting = false;
        considerarNPCs = false;
        contadorNPCs = 0;
    }

    public void BuscarNuevaRuta(NPC otroNPC)
    {
        if (m_movimiento.m_movActual != null && !rerouting)
        {
            tienePreferencia = false;
            m_movimiento.StopActual();
            if (corutinaAux != null) StopCoroutine(corutinaAux);
            StartCoroutine(Reroute(otroNPC.pathfinder));
        }
    }

    private IEnumerator Reroute(PathfinderNPC otroNPC)
    {
        rerouting = true;
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
            yield return new WaitWhile(() => enRuta);
            //Debug.Log("BACK ON TRACK!");
        }
        rerouting = false;
        m_movimiento.SiguienteAccion();
    }

    public void RutaTo(List<Vector2> objetivos, bool esPosReal)
    {
        npc.estadoActual = EstadoNPC.Caminando;
        enRuta = true;
        if (esPosReal) objetivos = Utilidades.GetPosicionesGrilla(objetivos, m_ciudad.transform);
        List<Vector2> posiciones = GetRuta(GetPosActualGrilla(), objetivos, rerouting);
        if (posiciones == null)
        {
            corutinaAux = StartCoroutine(Acercarse(GetObjetivoMasCercano(objetivos)));
        }
        else
        {
            if (!rerouting && considerarNPCs && posiciones.Count > 1 && !EsPosible(GetPosActualGrilla(), posiciones[1]))
            {
                tienePreferencia = false;
            }
            if (rerouting) rutaOriginal = objetivos;
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

    private IEnumerator Acercarse(Vector2 objetivo)
    {
        /*if (npc.nombre == "Pepe")
        {
            Debug.Log(GetPosActualGrilla());
            Debug.Log(objetivo);
        }*/
        if (pies.moviendo)
            yield return new WaitWhile(() => pies.moviendo);
        rutaActual.Clear();
        Vector2 objetivoIntermedio = GetObjetivoIntermedioPosible(objetivo);
        while (objetivoIntermedio != GetPosActualGrilla())
        {
            MoverOcupar(objetivoIntermedio);
            yield return new WaitWhile(() => pies.moviendo);
            objetivoIntermedio = GetObjetivoIntermedioPosible(objetivo);
        }
        npc.estadoActual = EstadoNPC.Quieto;
        if (objetivo == GetPosActualGrilla())
            enRuta = false;
        else corutinaAux = StartCoroutine("PedirPermiso", objetivo);
    }

    private Vector2 GetObjetivoIntermedioPosible(Vector2 objetivoFinal)
    {
        Vector2 distancia = objetivoFinal - GetPosActualGrilla();
        Vector2 objetivoIntermedio;
        if (distancia.x != 0)
        {
            objetivoIntermedio = GetPosActualGrilla() + new Vector2(Mathf.Sign(distancia.x), 0);
            if (EsPosible(GetPosActualGrilla(), objetivoIntermedio))
                return objetivoIntermedio;
        }
        if (distancia.y != 0)
        {
            objetivoIntermedio = GetPosActualGrilla() + new Vector2(0, Mathf.Sign(distancia.y));
            if (EsPosible(GetPosActualGrilla(), objetivoIntermedio))
                return objetivoIntermedio;
        }
        return GetPosActualGrilla();
    }

    private bool EsPosible(Vector2 posInicial, Vector2 posFinal)
    {
        return Utilidades.EsPosible(posInicial, posFinal, m_ciudad, true, this);
    }

    private IEnumerator PedirPermiso(Vector2 objetivo)
    {
        npc.estadoActual = EstadoNPC.Quieto;
        PathfinderNPC npcEsperado = null;
        Vector2 objetivoSiguiente = GetObjetivoSiguiente(GetPosActualGrilla(), objetivo);
        while (true)
        {
            npcEsperado = m_ciudad.NPCs.Find(x => x.posOcupadas.Contains(objetivoSiguiente));
            if (npcEsperado != null)
            {
                while (true)
                {
                    if (!npcEsperado.NpcCaminando() || EsPosible(GetPosActualGrilla(), objetivoSiguiente))
                        break;
                    yield return new WaitForSeconds(.1f);
                }
                yield return new WaitWhile(() => pies.moviendo);
                if (GetPosActualGrilla().x - objetivoSiguiente.x == 0 || GetPosActualGrilla().y - objetivoSiguiente.y == 0)
                {
                    if (!npcEsperado.NpcCaminando())
                    {
                        npcEsperado.StartCoroutine(DejarPasar(GetObjetivoSiguiente(objetivoSiguiente, objetivo)));
                        corutinaAux = StartCoroutine(EsperarNPC(objetivoSiguiente));
                        yield return new WaitWhile(() => npc.estadoActual == EstadoNPC.Esperando);
                    }
                    else if (EsPosible(GetPosActualGrilla(), objetivoSiguiente))
                        MoverOcupar(objetivoSiguiente);

                    yield return new WaitWhile(() => pies.moviendo);
                }


                if (objetivoSiguiente == objetivo)
                    enRuta = false;
                else
                {
                    if (GetPosActualGrilla().x - objetivo.x != 0 && GetPosActualGrilla().y - objetivo.y != 0)
                    {
                        if (corutinaAux == null)
                            RutaTo(new List<Vector2>() { objetivo }, false);
                    }
                    else
                    {
                        //corutinaAux = StartCoroutine("Acercarse", objetivo);
                        while (EsPosible(GetPosActualGrilla(), objetivoSiguiente))
                        {
                            MoverOcupar(objetivoSiguiente);
                            yield return new WaitWhile(() => pies.moviendo);
                            npc.estadoActual = EstadoNPC.Quieto;
                            if (GetPosActualGrilla() == objetivo)
                            {
                                enRuta = false;
                                yield break;
                            }
                            objetivoSiguiente = GetObjetivoSiguiente(GetPosActualGrilla(), objetivo);
                        }
                    }
                }
                yield break;
            }
            yield return new WaitForSeconds(.1f);
        }
    }

    private Vector2 GetObjetivoSiguiente(Vector2 posInicial, Vector2 objetivoFinal)
    {
        Vector2 distancia = objetivoFinal - posInicial;
        if (distancia.x != 0)
            return posInicial + new Vector2(Mathf.Sign(distancia.x), 0); ;
        if (distancia.y != 0)
            return posInicial + new Vector2(0, Mathf.Sign(distancia.y));
        return posInicial;
    }

    private IEnumerator DejarPasar(Vector2 posFinalOtro)
    {
        if (!NpcEntrando() && !EsFuturaPosicion(posFinalOtro))
        {
            Suelo sueloActual, sueloAux;
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
        return rutaActual.ContainsKey(posicion) && rutaActual[posicion] > Time.time;
    }

    private IEnumerator EsperarNPC(Vector2 objetivo)
    {
        npc.estadoActual = EstadoNPC.Esperando;
        Vector2 posInicial = GetPosActualGrilla();
        while (true)
        {
            if (posInicial != GetPosActualGrilla() || pies.moviendo)
            {
                if (pies.moviendo) yield return new WaitWhile(() => pies.moviendo);

                RutaTo(new List<Vector2>() { objetivo }, false);
                yield break;
            }
            else if (EsPosible(GetPosActualGrilla(), objetivo))
            {
                MoverOcupar(objetivo);
                yield break;
            }
            yield return new WaitForSeconds(.2f);
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

    private List<Vector2> GetRuta(Vector2 posInicial, List<Vector2> objetivos, bool considerarNPCS)
    {
        return Utilidades.GetVectoresRuta(posInicial, objetivos, m_ciudad, considerarNPCS, this);
    }

    private IEnumerator GoRuta(List<Vector2> posicionesGrilla)
    {
        Vector3 posInicial = Utilidades.GetPosicionReal(posicionesGrilla[0], m_ciudad.transform);
        if (transform.position != posInicial)
        {
            pies.Mover(posInicial);
            yield return new WaitWhile(() => pies.moviendo);
        }
        if (!rerouting) rutaOriginal = posicionesGrilla;
        SetRutaActual(posicionesGrilla);
        List<Vector2> nuevaRuta = null;
        int i = 1;
        while (i < posicionesGrilla.Count)
        {
            if (rutaActual[posicionesGrilla[i - 1]] + 0.05f < Time.time)
                SetRutaActual(posicionesGrilla.GetRange(i - 1, posicionesGrilla.Count - i + 1));
            //Esperar?
            if (posicionesGrilla[i] == posicionesGrilla[i - 1])
                yield return new WaitForSeconds(1 / pies.GetVelocidadMaximaReal);
            else
            {
                //Comprobar movimiento normal
                if (considerarNPCs && !tienePreferencia && !EsPosible(GetPosActualGrilla(), posicionesGrilla[i]))
                {
                    nuevaRuta = GetRuta(posicionesGrilla[i - 1], rutaOriginal, considerarNPCs);
                    if (nuevaRuta != null && nuevaRuta.Count > 1 && posicionesGrilla[i] != nuevaRuta[1])
                    {
                        Debug.Log("Ruta Alternativa");
                        corutinaAux = StartCoroutine(GoRuta(nuevaRuta));
                    }
                    else
                    {
                        //if (npc.nombre == "Pepe") Debug.Log("WAT");
                        corutinaAux = StartCoroutine(Acercarse(rutaOriginal.Last()));
                    }
                    yield break;
                }
                //Movimiento Normal
                pies.MoverOcupar(posicionesGrilla[i]);
                yield return new WaitWhile(() => pies.moviendo);
            }
            //Comprobar un mejor camino
            if (rerouting && i < posicionesGrilla.Count - 1)
            {
                nuevaRuta = GetRuta(posicionesGrilla[i], rutaOriginal, considerarNPCs);
                if (nuevaRuta != null && nuevaRuta.Count > 1 && posicionesGrilla[i + 1] != nuevaRuta[1] && nuevaRuta.Count < posicionesGrilla.Count - i)
                {
                    //Debug.Log("Mejor Ruta!");
                    corutinaAux = StartCoroutine(GoRuta(nuevaRuta));
                    yield break;
                }
            }
            i++;
        }
        enRuta = false;
        npc.estadoActual = EstadoNPC.Quieto;
        rutaActual.Clear();
    }

    public float CasillasPorSegundo()
    {
        return pies.GetVelocidadMaximaReal * 2;
    }

    private void MoverOcupar(Vector2 objetivo)
    {
        npc.estadoActual = EstadoNPC.Caminando;
        contadorEsperasRutaActual = 0;
        rutaActual.Clear();
        if (objetivo != GetPosActualGrilla())
            rutaActual.Add(GetPosActualGrilla(), Time.time);
        rutaActual.Add(objetivo, Time.time + (1 / CasillasPorSegundo()));
        pies.MoverOcupar(objetivo);
    }

    private void SetRutaActual(Vector2 objetivo)
    {
        contadorEsperasRutaActual = 0;
        rutaActual.Clear();
        if (objetivo != GetPosActualGrilla())
            rutaActual.Add(GetPosActualGrilla(), Time.time);
        rutaActual.Add(objetivo, Time.time + (1 / CasillasPorSegundo()));
    }

    private void SetRutaActual(List<Vector2> posicionesGrilla)
    {
        contadorEsperasRutaActual = 0;
        rutaActual.Clear();
        for (int p = 0; p < posicionesGrilla.Count; p++)
        {
            if (!rutaActual.ContainsKey(posicionesGrilla[p]))
                rutaActual.Add(posicionesGrilla[p], Time.time + (p / CasillasPorSegundo()));
            else contadorEsperasRutaActual++;
        }
    }
}
