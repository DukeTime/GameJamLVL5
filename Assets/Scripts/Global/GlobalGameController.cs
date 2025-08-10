public static class GlobalGameController
{
    public static bool Paused { get; private set; } = false;
    public static bool CutsceneFreezed { get; private set; } = false;

    public static void Pause()
    {
        Paused = true;
    }
    
    public static void Unpause()
    {
        Paused = false;
    }
    
    public static void CutsceneFreeze()
    {
        CutsceneFreezed = true;
    }
    
    public static void CutsceneUnfreeze()
    {
        CutsceneFreezed = false;
    }
}