            if (!AreStringListsEqual(oldData.ExtractedGames, newData.ExtractedGames))
            {
                var oldGames = string.Join(", ", (oldData.ExtractedGames ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)));
                var newGames = string.Join(", ", (newData.ExtractedGames ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)));
                changes.AddChange("ExtractedGames", TruncateForDisplay(oldGames, 200), TruncateForDisplay(newGames, 200));
            }
        }

        private static string TruncateForDisplay(string? value, int max)
        {
            var v = (value ?? string.Empty).Trim();
            if (v.Length <= max) return v;
            return v.Substring(0, Math.Max(0, max - 1)) + "…";
        }

        private static string FormatPrice(decimal? price)
        {
            if (price == null) return "-";
            return string.Format("{0:N0}", price.Value);
        }
        private static string FormatGuarantee(int? minutes)
        {
            if (minutes == null) return "-";
            var m = minutes.Value;
            if (m <= 0) return "0m";
            if (m % 60 == 0) return $"{m / 60}h";
            return $"{m}m";
        }
        private static bool AreStringListsEqual(IEnumerable<string>? a, IEnumerable<string>? b)
        {
            var aa = (a ?? Enumerable.Empty<string>()).Select(s => s?.Trim() ?? string.Empty).Where(s => s.Length > 0).OrderBy(s => s);
            var bb = (b ?? Enumerable.Empty<string>()).Select(s => s?.Trim() ?? string.Empty).Where(s => s.Length > 0).OrderBy(s => s);
            return aa.SequenceEqual(bb, StringComparer.OrdinalIgnoreCase);
        }
        private static ChangeType DetermineChangeType(ChangeDetails details)
        {
            // Heuristic: prioritize Sold/Status/Price, else generic Modified.
            var keys = details.Changes.Select(c => c.Field).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (keys.Contains("SoldStatus")) return ChangeType.StatusChanged;
            if (keys.Contains("PricePS4") || keys.Contains("PricePS5")) return ChangeType.PriceChanged;
            if (keys.Count == 0) return ChangeType.NoChange;
            return ChangeType.Modified;
        }
        private static string BuildShortSummary(string? oldText, string? newText)
        {
            var oldNorm = (oldText ?? string.Empty).Replace('\n', ' ');
            var newNorm = (newText ?? string.Empty).Replace('\n', ' ');
            var oldShort = TruncateForDisplay(oldNorm, 80);
            var newShort = TruncateForDisplay(newNorm, 80);
            if (string.IsNullOrEmpty(oldShort)) return newShort;
            return $"{oldShort} => {newShort}";
        }
    }
}
