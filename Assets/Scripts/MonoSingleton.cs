using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T m_Instance = null;
    public static T Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = GameObject.FindObjectOfType(typeof(T)) as T;
                m_Instance = m_Instance ?? new GameObject(typeof(T).ToString(), typeof(T)).GetComponent<T>();
                if (m_Instance == null)
                    Debug.LogWarning("MonoSingleton: Problem during the creation of " + typeof(T).ToString());
            }
            return m_Instance;
        }
    }

    protected virtual void Awake()
    {
        if (m_Instance == null)
            m_Instance = this as T;

        if (FindObjectsOfType(typeof(T)).Length > 1)
        {
            if (transform.parent == null)
                Destroy(gameObject);
            else Destroy(transform.parent.gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        m_Instance = null;
    }

    protected virtual void OnDestroy()
    {
        m_Instance = null;
    }
}