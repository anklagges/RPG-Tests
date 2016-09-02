using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EstadoNPC
{
    Quieto,
    Esperando,
    Caminando,
    Entrando,
    Saliendo,
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
    public EstadoNPC estadoActual { get; set; }
    public bool movimientosRandoms;

    //Auxiliares
    private PathfinderNPC pathFinder;
    private Ciudad ciudad;
    private const float limiteNecesidadHoras = 0.25f;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        pathFinder = this.GetComponent<PathfinderNPC>();
        ciudad = transform.parent.parent.GetComponent<Ciudad>();
        spriteRenderer = this.GetComponent<SpriteRenderer>();
        AgregarNecesidades();
        AgregarEdificiosUtiles();
        StartCoroutine("ComenzarNecesidades");
        StartCoroutine("DefinirDestino");
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
            if (necesidades.Exists(x => x.Nombre == edificio.necesidad))
            {
                Necesidad necesidad = necesidades.Find(x => x.Nombre == edificio.necesidad);
                necesidad.EdificioUtil = edificio;
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
                    necesidad.Valor -= 1;//TO.DO! Valor Necesidad
                    if (necesidad.Valor < 0) necesidad.Valor = 0;
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

    private IEnumerator DefinirDestino()
    {
        List<Edificio> edificiosUtiles = new List<Edificio>();
        yield return new WaitForSeconds(.5f);
        while (true)
        {
            if (estadoActual != EstadoNPC.Ocupado)
            {
                foreach (Necesidad necesidad in necesidades)
                {
                    if (necesidad.Valor <= Utilidades.HorasRealesToSecsJuego(limiteNecesidadHoras) && dinero >= necesidad.EdificioUtil.costo)
                    {
                        if (!pathFinder.edificiosObjetivos.Contains(necesidad.EdificioUtil))
                            edificiosUtiles.Add(necesidad.EdificioUtil);
                    }
                }
                if (edificiosUtiles.Count > 0)
                {
                    pathFinder.StartMoverToEdificios(edificiosUtiles);
                    edificiosUtiles.Clear();
                }
            }
            if (movimientosRandoms && estadoActual == EstadoNPC.Quieto)
                pathFinder.StartMoverRandom();
            yield return new WaitForSeconds(1);
        }
    }

    public void SatisfacerNecesidad(Edificio edificio)
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
