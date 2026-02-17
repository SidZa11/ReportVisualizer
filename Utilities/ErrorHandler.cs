using System;
using ReportVisualizer.Utilities;

namespace ReportVisualizer.Utilities
{
    public static class ErrorHandler
    {
        public static void HandleError(Exception ex, string message = null)
        {
            string errorMessage = message ?? "An unexpected error occurred.";
            Logger.LogError($"{errorMessage} Exception: {ex.Message} StackTrace: {ex.StackTrace}");
            // Optionally, re-throw the exception or perform other actions
        }
    }
}