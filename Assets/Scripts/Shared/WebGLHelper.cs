using System.Runtime.InteropServices;

public static class WebGLHelper
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SyncIndexedDB();
#endif

    public static void FlushSaveData()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SyncIndexedDB();
#endif
    }
}
