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
        this.Padre = null;
        this.Hijos = new List<TreeNode>();
        this.Data.Profundidad = 0;
    }

    public TreeNode(Data data, TreeNode padre)
    {
        this.Data = data;
        this.Padre = padre;
        this.Hijos = new List<TreeNode>();
        this.Data.Profundidad = this.Padre.Data.Profundidad + 1;
    }

    public TreeNode Clone()
    {
        return (TreeNode)this.MemberwiseClone();
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

    /*Se obtiene al crear los nodos ahora!, Comprobar cual es mas eficiente
    public int GetProfundidad()
    {
        int contador = 0;
        TreeNode nodo = this;
        while (nodo.Padre != null)
        {
            contador++;
            nodo = nodo.Padre;
        }
        return contador;
    }*/

    public float GetEvaluacion(string funcionEvaluativa, PathFinder pathFinder)
    {
        if (funcionEvaluativa == "Greedy")
            return EvaluacionGreedy();
        else if (funcionEvaluativa == "A*")
            return EvaluacionAStar();
        else if (funcionEvaluativa == "Depth")
            return EvaluacionDepth();
        else if (funcionEvaluativa == "Breadth")
            return EvaluacionBreadth(pathFinder);
        else return 0;
    }

    private float EvaluacionGreedy()
    {
        return Data.GetHeuristic();
    }

    private float EvaluacionAStar()
    {
        float costoAcumulado = 0;
        TreeNode nodo = this;
        do
        {
            costoAcumulado += nodo.Data.GetCostToNode(nodo.Padre);
            nodo = nodo.Padre;
        } while (nodo.Padre != null);
        return Data.GetHeuristic() + costoAcumulado;
    }

    private float EvaluacionDepth()
    {
        return -this.Data.Profundidad;
    }

    private float EvaluacionBreadth(PathFinder pathFinder)
    {
        return Math.Abs(this.Data.Profundidad - pathFinder.GetProfundidadActual());
    }
}