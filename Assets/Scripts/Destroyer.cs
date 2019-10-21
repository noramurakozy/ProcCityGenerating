using UnityEngine;

public class Destroyer : MonoBehaviour
{
    public static T SafeDestroy<T>(T obj) where T : UnityEngine.Object
    {
        if (Application.isEditor)
            DestroyImmediate(obj);
        else
            Destroy(obj);

        return null;
    }
    public static T SafeDestroyGameObject<T>(T component) where T : Component
    {
        if (component != null)
            SafeDestroy(component.gameObject);
        return null;
    }
}
