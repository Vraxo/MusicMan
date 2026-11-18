using MusicMan.Core;

namespace MusicMan.Orchestration;

internal static class TrackOutputCollisions
{
    public static IEnumerable<IGrouping<string, Track>> Find(IEnumerable<Track> tracks, string baseDataDir)
    {
        // Compare the default destinations, not the extensions of files already on disk.
        // Use a case-insensitive comparison so libraries are safe on case-insensitive volumes.
        return tracks
            .GroupBy(track => Path.GetFullPath(TrackProcessor.GetDefaultOutputFile(track, baseDataDir)), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Skip(1).Any());
    }
}
