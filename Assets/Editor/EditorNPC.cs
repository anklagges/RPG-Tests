using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(NPC))]
[CanEditMultipleObjects]
public class EditorNPC : Editor
{
    bool mostrarNecesidades = false;
    public override void OnInspectorGUI()
    {
        if (!EditorApplication.isPlaying)
            base.OnInspectorGUI();
        else
        {
            NPC npc = (NPC)target;
            //Nombre
            EditorGUILayout.TextField("Nombre", npc.nombre);

            //Dinero
            EditorGUILayout.TextField("Dinero", npc.dinero.ToString());

            //Estado Actual
            EditorGUILayout.LabelField("Estado Actual", npc.estadoActual.ToString());

            //Necesidades
            if (npc.necesidades != null)
            {
                /*mostrarNecesidades = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), mostrarNecesidades, (npc.necesidades != null ? "Necesidades" : "Necesidades Basicas"), true);
                if (mostrarNecesidades)
                {*/
                    foreach (Necesidad necesidad in npc.necesidades)
                        EditorGUILayout.LabelField(necesidad.Nombre, necesidad.Valor.ToString());
                    Repaint();
                //}
            }
        }
    }
}
