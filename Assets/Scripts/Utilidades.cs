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
}
