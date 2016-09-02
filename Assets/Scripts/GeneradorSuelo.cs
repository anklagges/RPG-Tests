using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GeneradorSuelo : MonoBehaviour
{
    public int alto;
    public int ancho;
    public const float sueloSize = 0.5f;

    //Auxiliares
    public Transform contenedorEntradas;
    public Ciudad m_ciudad;
    private GameObject[,] suelo;
    private List<Vector2> caminoPrincipalGrilla;

    //Prefabs
    public GameObject sueloPrefab;
    public Sprite marmol;
    public Sprite pasto;
    public Sprite tierra;
    public Sprite camino;
    public Sprite arena;

    void Start()
    {
        PathfinderNPC[] npcs = GameObject.FindObjectsOfType<PathfinderNPC>();
        foreach (PathfinderNPC npc in npcs)
        {
            if (npc.transform.parent.parent.name != transform.parent.name || !npc.gameObject.activeSelf) continue;
            m_ciudad.NPCs.Add(npc);
        }
        // Time.timeScale = DatosTest.multiplicadorEscalaTiempo;
        suelo = new GameObject[ancho, alto];
        m_ciudad.PosicionesActuales = new int[ancho, alto];
        GenerarPastoInicial();
        GenerarTierra();
        GenerarCaminoPrincipal();
        GenerarCaminosAdjacentes();
    }

    /*void Update()
    {
        Debug.Log(Time.time);
    }*/

    private void GenerarPastoInicial()
    {
        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                suelo[x, y] = Instantiate(sueloPrefab, Utilidades.GetPosicionReal(new Vector2(x, y), m_ciudad.transform), Quaternion.identity) as GameObject;
                suelo[x, y].GetComponent<SpriteRenderer>().sprite = pasto;
                suelo[x, y].name = "Pasto";
                suelo[x, y].transform.SetParent(transform);
                m_ciudad.PosicionesActuales[x, y] = (int)ESuelo.Pasto;
            }
        }
    }

    private void GenerarTierra()
    {
        Edificio[] edificios = GameObject.FindObjectsOfType<Edificio>();
        foreach (Edificio edificio in edificios)
        {
            if (edificio.transform.parent.name != transform.parent.name || !edificio.gameObject.activeSelf) continue;
            int posY = Mathf.RoundToInt(edificio.PosicionRelativa.y / sueloSize);
            int auxY = 0;
            if (edificio.PosicionRelativa.y % sueloSize >= sueloSize / 2) auxY--;
            int posX = Mathf.RoundToInt(edificio.PosicionRelativa.x / sueloSize);
            int auxX = 0;
            if (edificio.PosicionRelativa.x % sueloSize >= sueloSize / 2) auxX--;
            for (int y = posY + auxY; y < posY + edificio.Alto / sueloSize; y++)
            {
                for (int x = posX + auxX; x < posX + edificio.Ancho / sueloSize; x++)
                {
                    suelo[x, y].GetComponent<SpriteRenderer>().sprite = GetSpriteSuelo(edificio.suelo);
                    suelo[x, y].name = edificio.suelo + " - Edificio";
                    m_ciudad.PosicionesActuales[x, y] = 0;
                }
            }
        }
    }

    private Sprite GetSpriteSuelo(ESuelo suelo)
    {
        switch (suelo)
        {
            case ESuelo.Marmol: return marmol;
            case ESuelo.Camino: return camino;
            case ESuelo.Tierra: return tierra;
            case ESuelo.Pasto: return pasto;
            case ESuelo.Arena: return arena;
            default: return null;
        }
    }

    private void GenerarCaminoPrincipal()
    {
        caminoPrincipalGrilla = new List<Vector2>();
        int x, y;
        try
        {
            for (int i = 1; i < contenedorEntradas.childCount; i++)
            {
                foreach (Vector2 posicion in Utilidades.GetVectoresRuta(Utilidades.GetPosicionGrilla(contenedorEntradas.GetChild(0).position, m_ciudad.transform),
                    new List<Vector2>() { Utilidades.GetPosicionGrilla(contenedorEntradas.GetChild(i).position, m_ciudad.transform) }, m_ciudad, false, null))
                {
                    x = (int)posicion.x;
                    y = (int)posicion.y;
                    if (suelo[x, y].name != "Camino Principal")
                    {
                        suelo[x, y].GetComponent<SpriteRenderer>().sprite = camino;
                        suelo[x, y].name = "Camino Principal";
                        m_ciudad.PosicionesActuales[x, y] = (int)ESuelo.Camino;
                        caminoPrincipalGrilla.Add(new Vector2(x, y));
                    }
                    /*if (x - 1 >= 0 && suelo[x - 1, y].name == "Pasto" && suelo[x - 1, y].name != "Camino Principal")
                    {
                        suelo[x - 1, y].GetComponent<SpriteRenderer>().sprite = camino;
                        suelo[x - 1, y].name = "Camino Principal";
                        PosicionesActuales[x - 1, y] = Suelo.TraducirSuelo("Camino");
                        caminoPrincipalGrilla.Add(new Vector2(x - 1, y));
                    }
                    else if (y - 1 >= 0 && suelo[x, y - 1].name == "Pasto" && suelo[x, y - 1].name != "Camino Principal")
                    {
                        suelo[x, y - 1].GetComponent<SpriteRenderer>().sprite = camino;
                        suelo[x, y - 1].name = "Camino Principal";
                        PosicionesActuales[x, y - 1] = Suelo.TraducirSuelo("Camino");
                        caminoPrincipalGrilla.Add(new Vector2(x, y - 1));
                    }*/
                }
            }
        }
        catch
        {
            Debug.LogWarning("No hay ruta");
        }
    }

    private void GenerarCaminosAdjacentes()
    {
        int x, y;
        Edificio[] edificios = GameObject.FindObjectsOfType<Edificio>();
        foreach (Edificio edificio in edificios)
        {
            if (edificio.transform.parent.name != transform.parent.name || !edificio.gameObject.activeSelf) continue;
            try
            {
                foreach (Vector2 posicion in Utilidades.GetVectoresRuta(Utilidades.GetPosicionGrilla(edificio.Entrada, m_ciudad.transform), caminoPrincipalGrilla, m_ciudad, false, null))
                {
                    x = (int)posicion.x;
                    y = (int)posicion.y;
                    if (m_ciudad.PosicionesActuales[x, y] > (int)edificio.suelo)
                    {
                        suelo[x, y].GetComponent<SpriteRenderer>().sprite = GetSpriteSuelo(edificio.suelo);
                        suelo[x, y].name = edificio.suelo.ToString();
                        m_ciudad.PosicionesActuales[x, y] = (int)edificio.suelo;
                    }
                }
            }
            catch
            {
                Debug.LogWarning("No hay ruta");
            }
        }
    }
}
