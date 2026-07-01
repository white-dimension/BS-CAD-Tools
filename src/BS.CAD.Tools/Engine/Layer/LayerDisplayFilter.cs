using System;
using System.Collections.Generic;
using System.Linq;

namespace BS.CAD.Tools.Engine.Layer
{
    /// <summary>
    /// Pure logic filter for layer display lists.
    /// No WPF, no AutoCAD Transaction, no UI control references.
    /// </summary>
    public static class LayerDisplayFilter
    {
        /// <summary>
        /// Apply all filter stages in order:
        /// _cacheList → activeGroupLayerNames → activeFilterLayerNames → quickFilter → searchText
        /// </summary>
        /// <param name="source">Full layer list (_cacheList)</param>
        /// <param name="activeGroupLayerNames">Group filter (null/empty = no group filter)</param>
        /// <param name="activeFilterLayerNames">Custom tag filter (null/empty = no tag filter)</param>
        /// <param name="quickFilter">Quick filter: "All", "On", "Off", "Frozen", "Locked", "Current"</param>
        /// <param name="searchText">Search text (case-insensitive match on Name/ColorIndex/Linetype)</param>
        /// <returns>Filtered layer list</returns>
        public static List<T> Apply<T>(
            IEnumerable<T> source,
            HashSet<string>? activeGroupLayerNames,
            HashSet<string>? activeFilterLayerNames,
            string quickFilter,
            string searchText)
            where T : ILayerFilterItem
        {
            IEnumerable<T> query = source;

            // Stage 1: Group filter (highest priority)
            if (activeGroupLayerNames is { Count: > 0 })
            {
                var set = activeGroupLayerNames;
                query = query.Where(x => set.Contains(x.LayerName));
            }

            // Stage 2: Custom tag filter
            if (activeFilterLayerNames is { Count: > 0 })
            {
                var set = activeFilterLayerNames;
                query = query.Where(x => set.Contains(x.LayerName));
            }

            // Stage 3: Quick filter (state-based)
            query = quickFilter switch
            {
                "On" => query.Where(x => x.IsLayerOn),
                "Off" => query.Where(x => !x.IsLayerOn),
                "Frozen" => query.Where(x => x.IsLayerFrozen),
                "Locked" => query.Where(x => x.IsLayerLocked),
                "Current" => query.Where(x => x.IsLayerCurrent),
                _ => query
            };

            // Stage 4: Text search
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string s = searchText.ToLowerInvariant();
                query = query.Where(x =>
                    x.LayerName.ToLowerInvariant().Contains(s)
                    || x.LayerColorIndex.ToString().Contains(s)
                    || x.LayerLinetype.ToLowerInvariant().Contains(s)
                );
            }

            return query.ToList();
        }
    }

    /// <summary>
    /// Minimal interface for layer items to be filterable by LayerDisplayFilter.
    /// </summary>
    public interface ILayerFilterItem
    {
        string LayerName { get; }
        bool IsLayerOn { get; }
        bool IsLayerFrozen { get; }
        bool IsLayerLocked { get; }
        bool IsLayerCurrent { get; }
        short LayerColorIndex { get; }
        string LayerLinetype { get; }
    }
}
