using UnityEngine;

public class SingletonUnity<T> : MonoBehaviour
    where T : MonoBehaviour
{
    protected static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (T)FindObjectOfType(typeof(T));
                if (instance == null)
                {
                    Debug.LogError("An instance of " + typeof(T) + "need in scene, but get null");
                }
            }
            return instance;
        }
    }
}
