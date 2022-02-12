using HarmonyLib;
using Sandbox;

namespace MEPluginLoader.Patch
{
    [HarmonyPatch(typeof(MySandboxGame), "Update")]
    public static class Patch_Update
    {
        public static void Postfix()
        {
            if (Main.Instance != null)
            {
                Main.Instance.Update();
            }
        }
    }
}
