using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum ESuelo
{
    Edificio = 0,
    Personaje = 1,
    Camino = 2,
    Marmol = 3,
    Tierra = 4,
    Pasto = 5,
    Arena = 6,
}

public class Suelo : Data
{
    public PathfinderNPC NpcActual { get; set; }
    public Vector2 PosicionNPC { get; set; }
    private int ancho;
    private int alto;
    private List<Vector2> PosPlayerFinal;
    private Ciudad m_ciudad;
    private bool ConsiderarNPCs { get; set; }
    private int _profundidad;
    public const float c_margen = 0.05f;

    public Suelo(Vector2 posPlayerActual, Vector2 posPlayerFinal, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        this.m_ciudad = ciudad;
        PosPlayerFinal = new List<Vector2>();
        PosPlayerFinal.Add(posPlayerFinal);
        PosicionNPC = posPlayerActual;
        ConsiderarNPCs = considerarNPCs;
        NpcActual = npc;
        _profundidad = this.Profundidad;
        Init();
    }

    public Suelo(Vector2 posPlayerActual, List<Vector2> posPlayerFinal, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        m_ciudad = ciudad;
        PosPlayerFinal = posPlayerFinal;
        PosicionNPC = posPlayerActual;
        ConsiderarNPCs = considerarNPCs;
        NpcActual = npc;
        _profundidad = this.Profundidad;
        Init();
    }

    private void Init()
    {
        ancho = m_ciudad.PosicionesActuales.GetLength(0);
        alto = m_ciudad.PosicionesActuales.GetLength(1);
        Comparador = GetComparador();
    }

    private string GetComparador()
    {
        return PosicionNPC.ToString();
    }

    public override int GetHeuristic()
    {
        float aux;
        float menor = float.MaxValue;
        for (int i = 0; i < PosPlayerFinal.Count; i++)
        {
            Vector2 pos = PosPlayerFinal[i];
            aux = Mathf.Abs(PosicionNPC.x - pos.x) + Mathf.Abs(PosicionNPC.y - pos.y);
            if (aux < menor) menor = aux;
        }
        return (int)menor;
    }

    public override float GetCostToNode(TreeNode nodo)
    {
        Suelo sueloObjetivo = (Suelo)nodo.Data;
        if (sueloObjetivo.PosicionNPC == this.PosicionNPC) return 1;
        ESuelo tipoSuelo = (ESuelo)m_ciudad.PosicionesActuales[(int)sueloObjetivo.PosicionNPC.x, (int)sueloObjetivo.PosicionNPC.y];
        switch (tipoSuelo)
        {
            case ESuelo.Camino: return 1;
            case ESuelo.Marmol: return 1.25f;
            case ESuelo.Tierra: return 1.5f;
            case ESuelo.Pasto: return 2f;
            case ESuelo.Arena: return 4f;
            default: return 0;
        }
    }

    public override List<Data> GetPosibles()
    {
        int x = (int)PosicionNPC.x;
        int y = (int)PosicionNPC.y;
        List<Data> posiblesMovimientos = new List<Data>();
        if (x > 0 && EsPosible(x - 1, y)) posiblesMovimientos.Add(GetSueloNuevo(x - 1, y));
        if (y > 0 && EsPosible(x, y - 1)) posiblesMovimientos.Add(GetSueloNuevo(x, y - 1));
        if (x < ancho - 1 && EsPosible(x + 1, y)) posiblesMovimientos.Add(GetSueloNuevo(x + 1, y));
        if (y < alto - 1 && EsPosible(x, y + 1)) posiblesMovimientos.Add(GetSueloNuevo(x, y + 1));
        posiblesMovimientos.Add(GetSueloNuevo(x, y));
        return posiblesMovimientos;
    }

    public static bool EsPosible(Vector2 posInicial, Vector2 posObjetivo, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        Suelo suelo = new Suelo(posInicial, posObjetivo, ciudad, considerarNPCs, npc);
        return suelo.EsPosible((int)posObjetivo.x, (int)posObjetivo.y);
    }

    public bool EsPosible(int x, int y)
    {
        float tiempoAccion;
        PosRutaActual tiempoSwap, tiempoAux;
        //Checar que no sea edifio
        if (m_ciudad.PosicionesActuales[x, y] == 0) return false;

        DebugIf(x, y, "Es posible: " + x + "," + y);
        if (ConsiderarNPCs)
        {
            for (int i = 0; i < m_ciudad.NPCs.Count; i++)
            {
                PathfinderNPC otroNPC = m_ciudad.NPCs[i];
                if (otroNPC == NpcActual) continue;
                if (otroNPC.NpcCaminando())
                {
                    //Checar si calzan los tiempos estimados
                    if (otroNPC.rutaActual.TryGetValue(new Vector2(x, y), out tiempoAux))
                    {
                        tiempoAccion = Time.time + (1 + this.Profundidad) / NpcActual.CasillasPorSegundo();
                        //Checar que no vaya a haber otro npc en ese momento
                        float tiempoEntrada = tiempoAux.m_tiempo - c_margen - MedioTiempo(otroNPC);
                        float tiempoSalida = tiempoAux.m_tiempo + c_margen + MedioTiempo(otroNPC) * (1 + tiempoAux.m_vecesEsperado);
                        DebugIf(x, y, "DEBUG");
                        if (tiempoAccion >= tiempoEntrada && tiempoAccion <= tiempoSalida)
                        {
                            DebugIf(x, y, "NO");
                            return false;
                        }
                        //Checar si es Swap!
                        if (otroNPC.rutaActual.TryGetValue(PosicionNPC, out tiempoSwap))
                        {
                            //Checar que la ruta del npc no este contenida en la ruta del otro.
                            float tiempoInicialNPC = tiempoAccion - 2 * MedioTiempo(NpcActual);
                            //Se le agrega medio tiempo extra para permitir que salgan de sus tiles
                            float tiempoInicialOtro = tiempoSwap.m_tiempo - 3 * MedioTiempo(otroNPC);
                            float tiempoFinalOtro = tiempoSwap.m_tiempo + MedioTiempo(otroNPC);
                            if ((tiempoAccion <= tiempoFinalOtro && tiempoAccion >= tiempoInicialOtro) ||
                                (tiempoInicialNPC <= tiempoFinalOtro && tiempoInicialNPC >= tiempoInicialOtro))
                            {
                                DebugIf(x, y, "NO");
                                return false;
                            }
                        }
                        //Checar si la posicion a ocupar sera la ultima del otro npc y que llegara despues que el. (Dado que el otro npc se quedara parado ahi)
                        if (otroNPC.ultimaPosicion.m_posicion == new Vector2(x, y) &&
                            tiempoAccion > tiempoAux.m_tiempo + MedioTiempo(otroNPC))
                            return false;
                    }
                }
                else if (otroNPC.posOcupadas.Contains(new Vector2(x, y)))
                    return false;
            }
        }
        DebugIf(x, y, "SI");
        return true;
    }

    private void DebugIf(int x, int y, string mensaje)
    {
        if (x == 18 && y == 3)
        {
            //Debug.LogError(mensaje);
        }
    }

    private float MedioTiempo(PathfinderNPC npc)
    {
        return 1 / (2 * npc.CasillasPorSegundo());
    }

    private Suelo GetSueloNuevo(int nuevoX, int nuevoY)
    {
        return new Suelo(new Vector2(nuevoX, nuevoY), PosPlayerFinal, m_ciudad, ConsiderarNPCs, NpcActual);
    }

    public override bool Comparar(Data data)
    {
        Suelo grilla = (Suelo)data;
        return grilla.Comparador.Equals(this.Comparador);
    }
}
