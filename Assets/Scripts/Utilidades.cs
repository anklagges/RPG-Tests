using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Utilidades : MonoBehaviour
{
    public static int HorasRealesToSecsJuego(float horas)
    {
        //3600/1 = minuto / segundo
        //1/12 = real / juego
        return Mathf.RoundToInt(horas * 3600 / (12 * DatosTest.multiplicadorRelacionHoras));
    }
    /// <summary>
    /// Obtiene la posicion del mundo segun "posicionGrilla"
    /// </summary>
    /// <param name="posicionGrilla">Posicion en la grilla</param>
    /// <param name="ciudad">Ciudad contenedora</param>
    /// <returns></returns>
    public static Vector2 GetPosicionReal(Vector2 posicionGrilla, Transform ciudad)
    {
        float posY = posicionGrilla.y * GeneradorSuelo.sueloSize + GeneradorSuelo.sueloSize / 2f + ciudad.position.y - 0.01f;
        float posX = posicionGrilla.x * GeneradorSuelo.sueloSize + GeneradorSuelo.sueloSize / 2f + ciudad.position.x - 0.01f;
        return new Vector2(posX, posY);
    }

    public static Vector2 GetPosicionGrilla(Vector2 posicionReal, Transform ciudad)
    {
        int posY = Mathf.RoundToInt((posicionReal.y - ciudad.position.y) / GeneradorSuelo.sueloSize);
        int posX = Mathf.RoundToInt((posicionReal.x - ciudad.position.x) / GeneradorSuelo.sueloSize);
        return new Vector2(posX, posY);
    }

    public static List<Vector2> GetPosicionesGrilla(List<Vector2> posicionesReales, Transform ciudad)
    {
        List<Vector2> posicionesGrilla = new List<Vector2>();
        int posY, posX;
        for (int i = 0; i < posicionesReales.Count; i++)
        {
            posY = Mathf.RoundToInt((posicionesReales[i].y - ciudad.position.y) / GeneradorSuelo.sueloSize);
            posX = Mathf.RoundToInt((posicionesReales[i].x - ciudad.position.x) / GeneradorSuelo.sueloSize);
            posicionesGrilla.Add(new Vector2(posX, posY));
        }
        return posicionesGrilla;
    }

    public static Vector2 GetMovimientoRandom(Vector2 posInicial, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        Suelo suelo = new Suelo(posInicial, null, ciudad, considerarNPCs, npc);
        List<Data> posiblesLugares = suelo.GetPosibles();
        List<Vector2> movPosibles = new List<Vector2>();
        for (int i = 0; i < posiblesLugares.Count; i++)
        {
            suelo = (Suelo)posiblesLugares[i];
            if (ciudad.PosicionesActuales[(int)suelo.PosicionNPC.x, (int)suelo.PosicionNPC.y] != 1 &&
                ciudad.PosicionesActuales[(int)suelo.PosicionNPC.x, (int)suelo.PosicionNPC.y] < 5)
                movPosibles.Add(suelo.PosicionNPC);
        }
        //if (movPosibles.Count > 1)
        //    movPosibles.Remove(npc.ultimaPosicion);
        return movPosibles[Random.Range(0, movPosibles.Count)];
    }

    public static bool EsPosible(Vector2 posInicial, Vector2 posObjetivo, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc)
    {
        Suelo suelo = new Suelo(posInicial, posObjetivo, ciudad, considerarNPCs, npc);
        return suelo.EsPosible((int)posObjetivo.x, (int)posObjetivo.y);
    }

    public static List<Vector2> GetVectoresRuta(Vector2 posInicial, List<Vector2> posObjetivos, Ciudad ciudad, bool considerarNPCs, PathfinderNPC npc, out TreeNode nodoMasCercano)
    {
        PathFinder pathFinder = new PathFinder();
        pathFinder.SetNodoInicial(new Suelo(posInicial, posObjetivos, ciudad, considerarNPCs, npc));
        List<Suelo> posFinales = new List<Suelo>();
        for (int i = 0; i < posObjetivos.Count; i++)
            posFinales.Add(new Suelo(posObjetivos[i], posObjetivos[i], ciudad, considerarNPCs, npc));
        pathFinder.SetNodosFinales(posFinales.ToArray());
        List<TreeNode> ruta = pathFinder.GetRuta(false);
        nodoMasCercano = pathFinder.nodoMasCercano;
        DebugRuta(ruta);
        return TransformarRuta(ruta);
    }

    public static List<Vector2> TransformarRuta(List<TreeNode> ruta)
    {
        List<Vector2> posiciones = new List<Vector2>();
        Suelo sueloSiguiente;
        try
        {
            for (int i = 0; i < ruta.Count; i++)
            {
                sueloSiguiente = (Suelo)ruta[i].Data;
                posiciones.Add(sueloSiguiente.PosicionNPC);
            }
        }
        catch
        {
            return null;
        }
        return posiciones;
    }

    private static void DebugRuta(List<TreeNode> ruta)
    {
        if (ruta != null)
        {
            /*Suelo sueloAux;
            foreach (TreeNode nodo in ruta)
            {
                sueloAux = (Suelo)nodo.Data;
                Debug.Log(sueloAux.Comparador);
            }*/
            Debug.Log("Acciones Ruta: " + (ruta.Count - 1));
        }
        else
        {
            Debug.LogWarning("No se encontro una ruta!");
        }
    }
}
