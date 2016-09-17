using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EstadoNPC
{
    Quieto,
    Esperando,
    Caminando,
    Ocupado
}

//Relacion Hora real vs Hora Juego ==> 1 a 12
public class NPC : MonoBehaviour
{
    //Propiedades
    public string nombre;
    public string[] necesidadesIniciales;
    public int cantNecesidadesExtras;
    public int dinero;
    public List<Necesidad> necesidades;
    private List<Necesidad> necesidadesInsatisfechas = new List<Necesidad>();
    public EstadoNPC estadoActual { get; set; }
    public bool movimientosRandoms;
    public PosRutina[] m_rutina;
    public bool m_posRealesRutina;
    public float velocidadMaxima;

    //Componentes
    [HideInInspector]
    public Movimiento movimiento;
    [HideInInspector]
    public PathfinderNPC pathfinder;
    [HideInInspector]
    public Pies pies;
    [HideInInspector]
    public Ojos ojos;
    [HideInInspector]
    public BoxCollider2D col2D;

    //Prefabs
    public GameObject movimientoPrefab;
    public GameObject piesPrefab;
    public GameObject ojosPrefab;

    //Auxiliares
    [HideInInspector]
    public Ciudad ciudad;
    private const float limiteNecesidadHoras = 0.25f;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        ciudad = GetComponentInParent<Ciudad>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        movimiento = InstantiateComponent(movimientoPrefab).GetComponent<Movimiento>();
        pies = InstantiateComponent(piesPrefab).GetComponent<Pies>();
        ojos = InstantiateComponent(ojosPrefab).GetComponent<Ojos>();
        pathfinder = GetComponentInChildren<PathfinderNPC>();
        col2D = GetComponent<BoxCollider2D>();
        pies.Init();
        pathfinder.Init();
        movimiento.Init();
        ojos.Init();
        ciudad.NPCs.Add(pathfinder);
    }

    private GameObject InstantiateComponent(GameObject prefab)
    {
        GameObject component = Instantiate(prefab);
        component.transform.SetParent(transform);
        component.transform.localPosition = Vector3.zero;
        component.transform.localScale = Vector3.one;
        return component;
    }

    void Start()
    {
        AgregarNecesidades();
        AgregarEdificiosUtiles();
        StartCoroutine(ComenzarNecesidades());
        StartCoroutine(DefinirDestino());
    }

    private void AgregarNecesidades()
    {
        necesidades = new List<Necesidad>();
        //Agregar necesidades iniciales
        foreach (string necesidad in necesidadesIniciales)
            necesidades.Add(new Necesidad(necesidad, GetValorNecesidad()));
        //Agregar necesidades extras
        for (int i = 0; i < cantNecesidadesExtras; i++)
            necesidades.Add(new Necesidad(ciudad.GetNecesidadExtra(necesidades), GetValorNecesidad() + 6));//Bono temporal para test!
    }

    private int GetValorNecesidad()
    {
        return 3;
        //return Utilidades.HorasRealesToSecsJuego(Random.Range(6, 18));
    }

    private void AgregarEdificiosUtiles()
    {
        Edificio[] edificios = GameObject.FindObjectsOfType<Edificio>();
        foreach (Edificio edificio in edificios)
        {
            if (edificio.transform.parent.name != ciudad.name || !edificio.gameObject.activeSelf) continue;
            if (necesidades.Exists(x => x.Nombre == edificio.data.necesidad))
            {
                Necesidad necesidad = necesidades.Find(x => x.Nombre == edificio.data.necesidad);
                necesidad.EdificioUtil = edificio.data;
            }
        }
    }

    private IEnumerator ComenzarNecesidades()
    {
        while (true)
        {
            if (estadoActual != EstadoNPC.Ocupado)
            {
                foreach (Necesidad necesidad in necesidades)
                {
                    if (necesidad.Valor >= 1)
                    {
                        necesidad.Valor -= 1;//TO.DO! Valor Necesidad
                        if (!necesidadesInsatisfechas.Contains(necesidad) &&
                            necesidad.Valor <= Utilidades.HorasRealesToSecsJuego(limiteNecesidadHoras))
                        {
                            necesidadesInsatisfechas.Add(necesidad);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

    private IEnumerator DefinirDestino()
    {
        yield return new WaitForSeconds(.5f);

        if (necesidades.Count == 0 && m_rutina.Length > 0)
        {
            movimiento.StartRutina(m_rutina, m_posRealesRutina);
            yield break;
        }

        List<EdificioData> edificiosUtiles = new List<EdificioData>();
        while (true)
        {
            if (estadoActual != EstadoNPC.Ocupado)
            {
                for (int i = 0; i < necesidadesInsatisfechas.Count; i++)
                {
                    Necesidad necesidad = necesidadesInsatisfechas[i];
                    if (dinero >= necesidad.EdificioUtil.costo)
                    {
                        edificiosUtiles.Add(necesidad.EdificioUtil);
                        necesidadesInsatisfechas.RemoveAt(i);
                        i--;
                    }
                }
                if (edificiosUtiles.Count > 0)
                {
                    movimiento.UpdateEdificiosUtiles(edificiosUtiles);
                    edificiosUtiles.Clear();
                }
            }
            if (movimientosRandoms && estadoActual == EstadoNPC.Quieto)
                movimiento.StartRandom();
            yield return new WaitForSeconds(1);
        }
    }

    public void SatisfacerNecesidad(EdificioData edificio)
    {
        //TO.DO! Valor dependiende del edificio
        necesidades.Find(x => x.Nombre == edificio.necesidad).Valor += Utilidades.HorasRealesToSecsJuego(edificio.horasSatisfaccion);
        dinero -= edificio.costo;
    }

    private IEnumerator CambiarAlpha(float tiempo)
    {
        Color colorActual = spriteRenderer.color;
        int alphaNuevo = spriteRenderer.color.a == 1 ? 0 : 1;
        Color colorNuevo = new Color(colorActual.r, colorActual.g, colorActual.b, alphaNuevo);
        float t = 0;
        while (spriteRenderer.color.a != alphaNuevo)
        {
            t += Time.deltaTime / tiempo;
            spriteRenderer.color = Color.Lerp(colorActual, colorNuevo, t);
            yield return new WaitForFixedUpdate();
        }
    }
}
