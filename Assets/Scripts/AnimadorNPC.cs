using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class AnimadorNPC : MonoBehaviour
{
    private NPC npc;
    private Pies pies;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    public void Init()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        npc = GetComponentInParent<NPC>();
        pies = npc.pies;
    }

    void Update()
    {
        spriteRenderer.sortingOrder = 1000 + Mathf.RoundToInt(10 * -transform.position.y);
        if (npc == null) return;

        int horizontal = Math.Sign(pies.direccion.x);
        int vertical = Math.Sign(pies.direccion.y);
        anim.SetFloat("Horizontal", horizontal);
        anim.SetFloat("Vertical", vertical);
        anim.SetBool("Moviendo", pies.moviendo);
    }

    public void CambiarAlpha(float tiempo)
    {
        StartCoroutine(CoCambiarAlpha(tiempo));
    }

    IEnumerator CoCambiarAlpha(float tiempo)
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
