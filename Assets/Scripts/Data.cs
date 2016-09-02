using System.Collections.Generic;

public abstract class Data
{
    public string Comparador { get; set; }
    public float CostoAcumulado { get; set; }//Test si es mas eficiente
    public int Profundidad { get; set; }
    public abstract int GetHeuristic();
    public abstract float GetCostToNode(TreeNode nodo);
    public abstract List<Data> GetPosibles();
    public abstract bool Comparar(Data data);
}
