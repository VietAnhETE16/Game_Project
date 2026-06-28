using UnityEngine;

public static class UnityObjectFinder
{
    public static T[] FindAllActive<T>() where T : Object
    {
#if UNITY_6000_0_OR_NEWER
        return Object.FindObjectsByType<T>();
#elif UNITY_2022_2_OR_NEWER
        return Object.FindObjectsByType<T>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<T>();
#endif
    }

    public static T[] FindAllIncludingInactive<T>() where T : Object
    {
#if UNITY_6000_0_OR_NEWER
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include);
#elif UNITY_2022_2_OR_NEWER
        return Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<T>(true);
#endif
    }

    public static T FindAnyActive<T>() where T : Object
    {
        T[] objects = FindAllActive<T>();
        return objects.Length > 0 ? objects[0] : null;
    }
}
