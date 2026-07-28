using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            _instance = FindFirstObjectByType<T>();
            if (_instance != null)
                return _instance;

            GameObject singletonObject = new GameObject(typeof(T).Name);
            _instance = singletonObject.AddComponent<T>();
            return _instance;
        }
    }

    public static bool TryGetInstance(out T instance)
    {
        instance = _instance;
        return instance != null;
    }

    protected virtual bool PersistAcrossScenes => true;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (PersistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
