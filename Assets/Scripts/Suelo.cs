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
    private const float errorAbsoluto = 0.0001f;

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

    public bool EsPosible(int x, int y)
    {
        float tiempoAux, tiempoSwap, tiempoAccion;
        if (ConsiderarNPCs)
        {
            if (m_ciudad.PosicionesActuales[x, y] == 0) return false;
            for (int i = 0; i < m_ciudad.NPCs.Count; i++)
            {
                PathfinderNPC npc = m_ciudad.NPCs[i];
                if (npc == NpcActual) continue;
                if (npc.NpcCaminando())
                {

                    //Checar si calzan los tiempos estimados
                    if (npc.rutaActual.TryGetValue(new Vector2(x, y), out tiempoAux))
                    {
                        tiempoAccion = Time.time + (1 + this.Profundidad) / NpcActual.CasillasPorSegundo();
                        if (tiempoAccion >= tiempoAux - (1 / (2 * npc.CasillasPorSegundo())) && //- (1 / (2 * NpcActual.CasillasPorSegundo())) El npc espera que salga el otro, para empezar a entrar el
                            tiempoAccion <= tiempoAux + (1 / (2 * npc.CasillasPorSegundo())) + 0.05f)
                            return false;
                        //Checar si es Swap!
                        if (npc.rutaActual.TryGetValue(PosicionNPC, out tiempoSwap))
                        {
                            if (tiempoSwap > Time.time && Mathf.Abs(tiempoSwap - (tiempoAux + (1 / npc.CasillasPorSegundo()))) < errorAbsoluto)
                                return false;
                        }
                        if (npc.ultimaPosicion == new Vector2(x, y) && tiempoAccion > (tiempoAux + (1 / (2 * npc.CasillasPorSegundo())))) return false;
                    }
                }
                else if (npc.posOcupadas.Contains(new Vector2(x, y)))
                    return false;
            }
            return true;
        }
        return m_ciudad.PosicionesActuales[x, y] != 0;
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
