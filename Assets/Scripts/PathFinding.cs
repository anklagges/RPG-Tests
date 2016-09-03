using System.Collections.Generic;
using System.Linq;

public class PathFinder
{
    //Publicos
    public List<Data> DataLog { get; set; }
    public bool PermitirChicle { get; set; }
    public Dictionary<string, Data> PuntosClave { get; set; }
    public TreeNode nodoMasCercano;

    public void SetNodoInicial(Data data)
    {
        NodoInicial = new TreeNode(data);
    }

    public void SetNodosFinales(Data[] datas)
    {
        TreeNode nodoFinal;
        NodosFinales = new List<TreeNode>();
        for (int i = 0; i < datas.Length; i++)
        {
            nodoFinal = new TreeNode(datas[i]);
            NodosFinales.Add(nodoFinal);
            List<Data> posibles = nodoFinal.Data.GetPosibles();
            for (int j = 0; j < posibles.Count; j++)
            {
                Data puntoClave = posibles[j];
                if (!PuntosClave.ContainsKey(puntoClave.Comparador) && puntoClave.Comparador != nodoFinal.Data.Comparador)
                    PuntosClave.Add(puntoClave.Comparador, puntoClave);
            }
        }
    }

    //Auxiliares
    public List<TreeNode> NodosPosibles { get; set; }//Publico solo para tests
    public Dictionary<string, TreeNode> NodosVisitados { get; set; }
    private TreeNode NodoInicial { get; set; }
    private List<TreeNode> NodosFinales { get; set; }
    private TreeNode NodoActual { get; set; }

    private const int LimiteLoopInfinito = 200000;

    public PathFinder()
    {
        NodosVisitados = new Dictionary<string, TreeNode>();
        NodosPosibles = new List<TreeNode>();
        PuntosClave = new Dictionary<string, Data>();
        DataLog = new List<Data>();
    }

    public PathFinder(Data dataInicial, Data dataFinal)
    {
        SetNodoInicial(dataInicial);
        SetNodosFinales(new Data[] { dataFinal });
        NodosPosibles = new List<TreeNode>();
        NodosVisitados = new Dictionary<string, TreeNode>();
        DataLog = new List<Data>();
    }

    /// <summary>
    /// Obtiene la lista de nodos para llegar desde el nodo inicial al final
    /// </summary>
    /// <param name="FuncionEvaluativa">Posibles funciones Evaluativas: A*, Greedy, Breadth, Depth</param>
    /// <param name="permitirChicle">Permitir que vuelva a la posicion anterior de inmediato?</param>
    /// <returns></returns>
    public List<TreeNode> GetRuta(bool permitirChicle)
    {
        PermitirChicle = permitirChicle;
        return GetRuta();
    }

    public List<TreeNode> GetRuta()
    {
        Init();
        int contador = 0;
        nodoMasCercano = null;
        while (true)
        {
            if (contador == LimiteLoopInfinito) 
                return null;
            if (NodosFinales.Exists(x => x.Data.Comparar(NodoActual.Data)))
                return NodoActual.ObtenerRutaDesdeOrigen();
            if (PuntosClave.ContainsKey(NodoActual.Data.Comparador))
            {
                PuntosClave.Remove(NodoActual.Data.Comparador);
                if (PuntosClave.Count == 0) 
                    return null;
            }
            AgregarPosibles();
            if (NodosPosibles.Count == 0)
                return null;
            NodoActual = NodosPosibles[0];
            if (nodoMasCercano == null || NodoActual.EvaluacionGreedy() < nodoMasCercano.EvaluacionGreedy())
                nodoMasCercano = NodoActual;
            DataLog.Add(NodoActual.Data);
            NodosPosibles.RemoveAt(0);
            contador++;
        }
    }


    public void Init()
    {
        NodosVisitados.Clear();
        DataLog.Clear();
        NodosPosibles.Clear();
        NodoActual = NodoInicial;
    }

    public void AgregarPosibles()
    {
        TreeNode nodoPosible;
        foreach (Data posible in NodoActual.Data.GetPosibles())
        {
            nodoPosible = new TreeNode(posible, NodoActual);
            NodoActual.AgregarHijo(nodoPosible);
            if (ComprobarChicle(nodoPosible) || ComprobarVisitados(nodoPosible))
                continue;
            AgregarNodoToPosibles(nodoPosible);
            AgregarNodoToVisitados(nodoPosible);
        }
    }

    private bool ComprobarChicle(TreeNode nodo)
    {
        return !PermitirChicle && NodoActual.Padre != null && nodo.Data.Comparar(NodoActual.Padre.Data);
    }

    private bool ComprobarVisitados(TreeNode nodoPosible)
    {
        TreeNode nodoAux;
        if (NodosVisitados.TryGetValue(nodoPosible.Data.Comparador, out nodoAux))
        {
            if (nodoAux.EvaluacionAStar() < nodoPosible.EvaluacionAStar())
                return true;
            else
                NodosPosibles.Remove(nodoAux);
        }
        return false;
    }

    private void AgregarNodoToPosibles(TreeNode nodoAgregado)
    {
        float evaluacionAgregado = nodoAgregado.EvaluacionAStar();
        if (NodosPosibles.Count > 0 && evaluacionAgregado < NodosPosibles.Last().EvaluacionAStar())
        {
            int indiceAux = NodosPosibles.FindIndex(x => x.EvaluacionAStar() >= evaluacionAgregado);
            NodosPosibles.Insert(indiceAux, nodoAgregado);
        }
        else NodosPosibles.Add(nodoAgregado);
    }

    private void AgregarNodoToVisitados(TreeNode nodoAgregado)
    {
        if (NodosVisitados.ContainsKey(nodoAgregado.Data.Comparador))
            NodosVisitados[nodoAgregado.Data.Comparador] = nodoAgregado;
        else NodosVisitados.Add(nodoAgregado.Data.Comparador, nodoAgregado);
    }

    public int GetProfundidadActual()
    {
        return this.NodoActual.Data.Profundidad;
    }
}