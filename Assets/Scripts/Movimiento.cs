using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Movimiento : MonoBehaviour
{
    public List<Edificio> edificiosObjetivos = new List<Edificio>();

    private NPC npc;
    private PathfinderNPC m_pathfinder;
    private BoxCollider2D col2D;
    private Pies pies;

    //Auxiliares
    private Ciudad m_ciudad;
    public Vector2 objetivoRandom;
    public Coroutine m_movActual;

    public bool saliendoEdificio;

    public void Init()
    {
        npc = GetComponentInParent<NPC>();
        col2D = npc.col2D;
        m_ciudad = npc.ciudad;
        m_pathfinder = npc.pathfinder;
        pies = npc.pies;
    }

    private void PausarAnterior()
    {
        StopActual();
        m_pathfinder.Stop();
    }

    public void StopActual()
    {
        if (m_movActual != null) StopCoroutine(m_movActual);
    }

    public void SiguienteAccion()
    {
        if (npc.estadoActual != EstadoNPC.Entrando && npc.estadoActual != EstadoNPC.Ocupado)
        {
            if (edificiosObjetivos.Count > 0) m_movActual = StartCoroutine(MoverToEdificios());
            else if (npc.movimientosRandoms) m_movActual = StartCoroutine(MoverRandom());
        }
        else npc.estadoActual = EstadoNPC.Quieto;
    }

    #region Movimiento Random
    public void StartMoverRandom()
    {
        //Debug.Log("MoverRandom");
        PausarAnterior();
        objetivoRandom = m_ciudad.GetCaminoRandom();
        m_movActual = StartCoroutine(MoverRandom());
    }

    IEnumerator MoverRandom()
    {
        m_pathfinder.RutaTo(new List<Vector2>() { objetivoRandom }, false);
        yield return new WaitWhile(() => m_pathfinder.enRuta);
    }
    #endregion

    #region Movimiento Edificio
    public void StartMoverToEdificios(List<Edificio> edificios)
    {
        if (npc.estadoActual != EstadoNPC.Entrando && npc.estadoActual != EstadoNPC.Ocupado)
        {
            PausarAnterior();
            edificiosObjetivos.AddRange(edificios);
            m_movActual = StartCoroutine(MoverToEdificios());
        }
    }

    IEnumerator MoverToEdificios()
    {
        List<Vector2> posiciones = new List<Vector2>();
        edificiosObjetivos.ForEach(x => posiciones.Add(x.Entrada));
        yield return new WaitWhile(() => pies.moviendo);
        m_pathfinder.RutaTo(posiciones, true);
        yield return new WaitWhile(() => m_pathfinder.enRuta);
        m_movActual = null;
        m_pathfinder.rutaOriginal.Clear();
        StartCoroutine(EntrarEdificio());
    }

    IEnumerator EntrarEdificio()
    {
        npc.estadoActual = EstadoNPC.Entrando;
        Edificio edificioObjetivo = GetEdificioObjetivo();
        //Ponerse al frente de la entrada
        if (edificioObjetivo.Entrada != transform.position)
        {
            pies.Mover(edificioObjetivo.Entrada);
            yield return new WaitWhile(() => pies.moviendo);
        }
        Entrar(edificioObjetivo);
        yield return new WaitWhile(() => pies.moviendo);
        edificioObjetivo.entradaLibre = true;
        m_pathfinder.EntrarEdificio();
        //Satisfacer Necesidad
        npc.estadoActual = EstadoNPC.Ocupado;
        yield return new WaitForSeconds(Utilidades.HorasRealesToSecsJuego(edificioObjetivo.horasDuracion));
        npc.SatisfacerNecesidad(edificioObjetivo);
        //Salir
        saliendoEdificio = true;
        Vector2 posSalida = Utilidades.GetPosicionGrilla(edificioObjetivo.Entrada, m_ciudad.transform);
        yield return new WaitUntil(() => edificioObjetivo.entradaLibre && m_pathfinder.ComprobarObjetivo(posSalida));
        npc.estadoActual = EstadoNPC.Caminando;
        Vector3 posSalidaReal = Utilidades.GetPosicionReal(posSalida, m_ciudad.transform);
        pies.SetEnabledCol(true);
        col2D.enabled = true;
        m_pathfinder.SalirEdificio(posSalida);
        pies.Mover(posSalidaReal);
        npc.StartCoroutine("CambiarAlpha", (1 / (2 * pies.GetVelocidadMaximaReal)));
        edificioObjetivo.entradaLibre = false;
        yield return new WaitWhile(() => pies.moviendo);
        npc.estadoActual = EstadoNPC.Quieto;
        edificioObjetivo.entradaLibre = true;
        edificiosObjetivos.Remove(edificioObjetivo);
        saliendoEdificio = false;
        m_pathfinder.ClearRutaActual();
        SiguienteAccion();
    }

    private void Entrar(Edificio edificioObjetivo)
    {
        pies.Mover(edificioObjetivo.Entrada + new Vector3(0, 0.5f));
        npc.StartCoroutine("CambiarAlpha", (1 / (2 * pies.GetVelocidadMaximaReal)));
        m_pathfinder.posOcupadas.Clear();
        col2D.enabled = false;
        pies.SetEnabledCol(false);
        edificioObjetivo.entradaLibre = false;
    }

    public Edificio GetEdificioObjetivo()
    {
        Edificio edificioObjetivo = null;
        float distancia;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < edificiosObjetivos.Count; i++)
        {
            distancia = Vector2.Distance(edificiosObjetivos[i].Entrada, transform.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                edificioObjetivo = edificiosObjetivos[i];
            }
        }

        return edificioObjetivo;
    }
    #endregion
}
