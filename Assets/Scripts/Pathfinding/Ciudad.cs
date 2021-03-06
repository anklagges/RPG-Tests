﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Ciudad : MonoBehaviour
{
    public string nombre;
    public string[] necesidadesDisponibles;
    public int[,] PosicionesActuales { get; set; }
    public List<PathfinderNPC> NPCs;

    public Vector2 GetCaminoRandom()
    {
        List<Vector2> caminos = new List<Vector2>();
        for (int x = 0; x < PosicionesActuales.GetLength(0); x++)
        {
            for (int y = 0; y < PosicionesActuales.GetLength(1); y++)
            {
                if ((ESuelo)PosicionesActuales[x, y] == ESuelo.Camino)
                    caminos.Add(new Vector2(x, y));
            }
        }
        return caminos[Random.Range(0, caminos.Count)];
    }

    /// <summary>
    /// Devuelve una necesidad disponible en la ciudad, que no este en la lista pasada
    /// </summary>
    /// <param name="necesidadesActuales"></param>
    /// <returns></returns>
    public string GetNecesidadExtra(List<Necesidad> necesidadesActuales)
    {
        string[] necesidadesPosibles = necesidadesDisponibles.Where(x => !necesidadesActuales.Exists(y => y.Nombre == x)).ToArray();
        if (necesidadesPosibles.Length == 0) return "";
        int indiceElegida = Random.Range(0, necesidadesPosibles.Length);
        return necesidadesPosibles[indiceElegida];
    }
}
