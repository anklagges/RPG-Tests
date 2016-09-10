using System;
using System.Collections;
using System.Collections.Generic;

public class TreeNode
{
    public Data Data { get; set; }
    public TreeNode Padre { get; set; }
    public List<TreeNode> Hijos { get; set; }

    public TreeNode(Data data)
    {
        this.Data = data;
        this.Data.Padre = null;
        this.Padre = null;
        this.Hijos = new List<TreeNode>();
        this.Data.Profundidad = 0;
    }

    public TreeNode(Data data, TreeNode padre)
    {
        this.Data = data;
        this.Data.Padre = padre.Data;
        this.Padre = padre;
        this.Hijos = new List<TreeNode>();
        this.Data.Profundidad = this.Padre.Data.Profundidad + 1;
    }

    public void AgregarHijo(TreeNode hijo)
    {
        this.Hijos.Add(hijo);
    }

    public List<TreeNode> ObtenerRutaDesdeOrigen()
    {
        List<TreeNode> nodosRuta = new List<TreeNode>();
        TreeNode nodo = this;
        do
        {
            nodosRuta.Add(nodo);
            nodo = nodo.Padre;
        } while (nodo != null);
        nodosRuta.Reverse();
        return nodosRuta;
    }

    public float EvaluacionGreedy()
    {
        return Data.GetHeuristic();
    }

    public float EvaluacionAStar()
    {
        return Data.GetHeuristic() + Data.CostoAcumulado;
    }
}