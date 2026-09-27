using System.Collections.Generic;

namespace XerahS.RegionCapture.ScreenRecording;

/// <summary>
/// Centralizes which screen-recording codecs are directly supported by the native backend
/// versus requiring the FFmpeg fallback in the current build.
/// </summary>
public static class RecordingCodecSupportPolicy
{
    private static readonly IReadOnlyList<VideoCodec> AllCodecs = new[]
    {
        VideoCodec.H264,
        VideoCodec.HEVC,
        VideoCodec.VP9,
        VideoCodec.AV1
    };

    /// <summary>
    /// The Linux recording backend encodes every codec itself, with or without FFmpeg installed.
    /// </summary>
    public static IReadOnlyList<VideoCodec> GetSelectableCodecs(bool ffmpegAvailable)
    {
        return AllCodecs;
    }

    public static bool RequiresFfmpegFallback(VideoCodec codec)
    {
        return false;
    }
}
