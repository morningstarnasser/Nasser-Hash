using System.Collections.Generic;

namespace HashcatGUI.Services;

/// <summary>
/// Service for GPU optimization settings for Hashcat.
/// </summary>
public static class GpuOptimizationService
{
    /// <summary>
    /// Gets optimized Hashcat arguments based on GPU settings.
    /// </summary>
    public static List<string> GetOptimizedArguments(GpuProfile profile)
    {
        var args = new List<string>();

        switch (profile)
        {
            case GpuProfile.Conservative:
                // Low power, safe temperatures
                args.Add("-w 1"); // Workload profile: Low
                args.Add("--gpu-temp-abort=85");
                args.Add("--gpu-temp-retain=75");
                break;

            case GpuProfile.Balanced:
                // Default balanced settings
                args.Add("-w 2"); // Workload profile: Default
                args.Add("--gpu-temp-abort=90");
                args.Add("--gpu-temp-retain=80");
                break;

            case GpuProfile.Performance:
                // Maximum performance
                args.Add("-w 3"); // Workload profile: High
                args.Add("--gpu-temp-abort=95");
                args.Add("--gpu-temp-retain=85");
                args.Add("-O"); // Optimized kernels
                break;

            case GpuProfile.Insane:
                // All-out attack (may cause instability)
                args.Add("-w 4"); // Workload profile: Nightmare
                args.Add("--gpu-temp-abort=100");
                args.Add("-O"); // Optimized kernels
                args.Add("--force"); // Ignore warnings
                break;
        }

        // Common optimizations
        args.Add("--status");
        args.Add("--status-timer=10");

        return args;
    }

    /// <summary>
    /// Gets recommended workload size based on GPU memory.
    /// </summary>
    public static string GetRecommendedWorkload(int gpuMemoryMB)
    {
        if (gpuMemoryMB >= 12000)
            return "4"; // Very high for 12GB+ GPUs
        if (gpuMemoryMB >= 8000)
            return "3"; // High for 8GB+ GPUs
        if (gpuMemoryMB >= 4000)
            return "2"; // Default for 4GB+ GPUs
        return "1"; // Low for smaller GPUs
    }

    /// <summary>
    /// Gets profile description.
    /// </summary>
    public static string GetProfileDescription(GpuProfile profile)
    {
        return profile switch
        {
            GpuProfile.Conservative => "Niedrige Last - Sicher für alte GPUs, niedrige Temperaturen",
            GpuProfile.Balanced => "Ausgewogen - Gute Performance bei sicheren Temperaturen",
            GpuProfile.Performance => "Hohe Leistung - Maximum Speed, höhere Temperaturen",
            GpuProfile.Insane => "Insane - Absolute Maximum, Überhitzungsgefahr!",
            _ => "Unbekannt"
        };
    }

    /// <summary>
    /// Gets estimated speed multiplier compared to balanced.
    /// </summary>
    public static double GetSpeedMultiplier(GpuProfile profile)
    {
        return profile switch
        {
            GpuProfile.Conservative => 0.6,
            GpuProfile.Balanced => 1.0,
            GpuProfile.Performance => 1.4,
            GpuProfile.Insane => 1.8,
            _ => 1.0
        };
    }
}

public enum GpuProfile
{
    Conservative,
    Balanced,
    Performance,
    Insane
}
