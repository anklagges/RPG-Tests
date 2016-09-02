using UnityEngine;
using System.Collections;

public class Necesidad
{
    public Necesidad(string nombre, int valor)
    {
        Nombre = nombre;
        Valor = valor;
        EdificioUtil = null;
    }
    public string Nombre;
    public int Valor;
    public Edificio EdificioUtil;//TO.DO! Multiples edificios
}