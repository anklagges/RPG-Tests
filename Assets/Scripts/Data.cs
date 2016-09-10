using System.Collections.Generic;

public abstract class Data
{
    public abstract Data Padre { get; set; }
    public string Comparador { get; set; }
    public float CostoAcumulado { get; set; }
    public int Profundidad { get; set; }
    public abstract int GetHeuristic();
    public abstract List<Data> GetPosibles();
    public abstract bool Comparar(Data data);
}
