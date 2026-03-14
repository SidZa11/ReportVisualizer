
namespace ReportVisualizer.Security
{
    public static class ModeManager
    {
        private const string SessionKey = "AppMode";

        public static bool IsDev(HttpContext context)
        {
            var mode = context.Session.GetString(SessionKey);
            Console.WriteLine("Session Mode Read = " + mode);
            return mode == "DEV";
        }


        public static void SetDev(HttpContext context)
        {
            context.Session.SetString(SessionKey, "DEV");
        }

        public static void Logout(HttpContext context)
        {
            context.Session.Remove(SessionKey);
        }
    }
}