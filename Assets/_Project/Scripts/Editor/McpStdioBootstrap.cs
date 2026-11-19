using System;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Development tooling only. This project connects Claude Code to the editor through MCP for Unity in
    /// stdio mode (see .mcp.json), but the package defaults to HTTP. On editor load this switches the
    /// package to stdio once and starts its bridge. Uses reflection so the project still compiles if the
    /// MCP package is removed.
    /// </summary>
    [InitializeOnLoad]
    internal static class McpStdioBootstrap
    {
        private const string HttpPrefKey = "MCPForUnity.UseHttpTransport";
        private const string DonePrefKey = "LastSignal.McpStdioConfigured";

        static McpStdioBootstrap()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += Configure;
        }

        private static void Configure()
        {
            var cacheType = Type.GetType("MCPForUnity.Editor.Services.EditorConfigurationCache, MCPForUnity.Editor");
            var bridgeType = Type.GetType("MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost, MCPForUnity.Editor");
            if (cacheType == null || bridgeType == null) return; // package not installed

            if (!EditorPrefs.GetBool(DonePrefKey, false))
            {
                EditorPrefs.SetBool(HttpPrefKey, false);
                EditorPrefs.SetBool(DonePrefKey, true);
                try
                {
                    var instance = cacheType.GetProperty("Instance")?.GetValue(null);
                    cacheType.GetMethod("Refresh")?.Invoke(instance, null);
                    bridgeType.GetMethod("StartAutoConnect")?.Invoke(null, null);
                    Debug.Log("[McpStdioBootstrap] MCP for Unity switched to stdio transport and bridge started.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[McpStdioBootstrap] Could not start the MCP bridge automatically: {e.Message}");
                }
            }
        }
    }
}
