namespace Flowers_Series.Common
{
    using SharpDX;
    using System.Collections.Generic;

    internal static class WaypointTracker
    {
        public static readonly Dictionary<int, List<Vector2>> StoredPaths = new Dictionary<int, List<Vector2>>();
        public static readonly Dictionary<int, int> StoredTick = new Dictionary<int, int>();
    }
}
