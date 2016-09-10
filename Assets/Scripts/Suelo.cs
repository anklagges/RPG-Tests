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
    private int m_profundidad;
    private float m_tiempoAcumulado;
    public const float c_margen = 0.05f;
    private Data m_padre;
    public override Data Padre
    {
        get { return m_padre; }
        set
        {
            m_padre = value;
            if (value != null) InitPadreValues();
        }
    }

    public Suelo(Vector2 posPlayerActual, Vector2 posPlayerFinal, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        m_ciudad = ciudad;
        PosPlayerFinal = new List<Vector2>();
        PosPlayerFinal.Add(posPlayerFinal);
        PosicionNPC = posPlayerActual;
        ConsiderarNPCs = considerarNPCs;
        NpcActual = npc;
        Init();
    }

    public Suelo(Vector2 posPlayerActual, List<Vector2> posPlayerFinal, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        m_ciudad = ciudad;
        PosPlayerFinal = posPlayerFinal;
        PosicionNPC = posPlayerActual;
        ConsiderarNPCs = considerarNPCs;
        NpcActual = npc;
        Init();
    }

    private void Init()
    {
        if (Padre == null)
        {
            CostoAcumulado = 0;
            m_tiempoAcumulado = Time.time;
        }
        else InitPadreValues();
        m_profundidad = this.Profundidad;
        ancho = m_ciudad.PosicionesActuales.GetLength(0);
        alto = m_ciudad.PosicionesActuales.GetLength(1);
        Comparador = GetComparador();
    }

    private void InitPadreValues()
    {
        CostoAcumulado = m_padre.CostoAcumulado + GetCostToNode((Suelo)m_padre);
        if (NpcActual != null)
        {
            ESuelo tipo = (ESuelo)m_ciudad.PosicionesActuales[(int)PosicionNPC.x, (int)PosicionNPC.y];
            m_tiempoAcumulado = ((Suelo)Padre).m_tiempoAcumulado + Pies.m_resistenciasSuelo[tipo.ToString()] * NpcActual.TiempoPorCasilla(PosicionNPC);
        }
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

    private float GetCostToNode(Suelo suelo)
    {
        if (suelo.PosicionNPC == this.PosicionNPC) return 0.4f;
        ESuelo tipoSuelo = (ESuelo)m_ciudad.PosicionesActuales[(int)suelo.PosicionNPC.x, (int)suelo.PosicionNPC.y];
        switch (tipoSuelo)
        {
            case ESuelo.Marmol: return 0.9f;
            case ESuelo.Camino: return 1;
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

    public static bool RutasCruzadas(List<Vector2> rutaA, List<Vector2> rutaB, Ciudad ciudad, PathfinderNPC npcA, PathfinderNPC npcB)
    {
        Suelo suelo;
        if (npcB.ultimaPosicion == null)
            return true;
        Vector2 ultimaPos = npcB.ultimaPosicion.m_posicion;
        for (int a = 0; a < rutaA.Count; a++)
        {
            for (int b = 0; b < rutaB.Count; b++)
            {
                if (Mathf.Abs(rutaA[a].x - rutaB[b].x) + Mathf.Abs(rutaA[a].y - rutaB[b].y) <= 1)
                {
                    suelo = new Suelo(rutaA[a], ultimaPos, ciudad, true, npcA);
                    if (!suelo.EsPosible(npcB, rutaB[b]))
                    {
                        //Debug.LogError(rutaA[a] + " -> " + rutaB[b]);
                        return true;
                    }
                }
            }
        }
        //Debug.LogError("NO CRUZAN");
        return false;
    }

    public static bool EsPosible(Vector2 posInicial, Vector2 posObjetivo, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        Suelo suelo = new Suelo(posInicial, posObjetivo, ciudad, considerarNPCs, npc);
        return suelo.EsPosible(posObjetivo);
    }

    private bool EsPosible(int x, int y)
    {
        return EsPosible(new Vector2(x, y));
    }

    public bool EsPosible(Vector2 pos)
    {
        //Checar que no sea edifio
        int x = (int)pos.x;
        int y = (int)pos.y;
        if (m_ciudad.PosicionesActuales[x, y] == 0) return false;

        //DebugIf(pos, "Es posible: " + x + "," + y);
        if (ConsiderarNPCs)
        {
            for (int i = 0; i < m_ciudad.NPCs.Count; i++)
            {
                PathfinderNPC otroNPC = m_ciudad.NPCs[i];
                if (otroNPC == NpcActual) continue;
                if (otroNPC.NpcCaminando())
                {
                    if (!EsPosible(otroNPC, pos)) return false;
                }
                else if (otroNPC.posOcupadas.Contains(pos))
                    return false;
            }
        }
        //DebugIf(pos, "SI TOTAL");
        return true;
    }

    public bool EsPosible(PathfinderNPC otroNPC, Vector2 pos)
    {
        float tiempoAccion;
        PosRutaActual tiempoSwap, tiempoAux;
        //Checar si calzan los tiempos estimados
        if (otroNPC.rutaActual.TryGetValue(pos, out tiempoAux))
        {
            tiempoAccion = m_tiempoAcumulado + NpcActual.TiempoPorCasilla(pos);
            //Checar que no vaya a haber otro npc en ese momento
            float medioTiempoOtro = MedioTiempo(otroNPC, pos);
            float tiempoEntrada = tiempoAux.m_tiempo - c_margen - medioTiempoOtro;
            float tiempoSalida = tiempoAux.m_tiempo + c_margen + medioTiempoOtro * (1 + tiempoAux.m_vecesEsperado);
            //DebugIf(pos, "DEBUG");
            if (tiempoAccion >= tiempoEntrada && tiempoAccion <= tiempoSalida)
            {
                DebugIf(pos, "NO NORMAL");
                return false;
            }
            //Checar si es Swap!
            if (otroNPC.rutaActual.TryGetValue(PosicionNPC, out tiempoSwap))
            {
                //Checar que la ruta del npc no este contenida en la ruta del otro.
                float tiempoInicialNPC = tiempoAccion - 2 * MedioTiempo(NpcActual, PosicionNPC);
                //Se le agrega medio tiempo extra para permitir que salgan de sus tiles
                float tiempoInicialOtro = tiempoSwap.m_tiempo - 3 * medioTiempoOtro;
                float tiempoFinalOtro = tiempoSwap.m_tiempo + medioTiempoOtro;
                if ((tiempoAccion <= tiempoFinalOtro && tiempoAccion >= tiempoInicialOtro) ||
                    (tiempoInicialNPC <= tiempoFinalOtro && tiempoInicialNPC >= tiempoInicialOtro))
                {
                    DebugIf(pos, "NO SWAP");
                    return false;
                }
            }
            //Checar si la posicion a ocupar sera la ultima del otro npc y que llegara despues que el. (Dado que el otro npc se quedara parado ahi)
            if (otroNPC.ultimaPosicion.m_posicion == pos &&
                tiempoAccion > tiempoAux.m_tiempo + medioTiempoOtro)
            {
                DebugIf(pos, "ULTIMA POS");
                return false;
            }
        }
        //DebugIf(pos, "SI");
        return true;
    }

    private void DebugIf(Vector2 pos, string mensaje)
    {
        // if (pos.x == 18 && pos.y == 3)
        {
            //Debug.LogError("POS: " + pos + " -> " + mensaje);
        }
    }

    private float MedioTiempo(PathfinderNPC npc, Vector2 pos)
    {
        return npc.TiempoPorCasilla(pos) / 2f;
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
