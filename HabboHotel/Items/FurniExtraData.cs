using System.Text.RegularExpressions;
using Plus.HabboHotel.Items.DataFormat;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Items;

internal static class FurniExtraData
{
    private static readonly HashSet<string> ClientImageKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "w", "url", "id", "imageUrl", "href", "src", "clickUrl"
    };

    private static readonly Regex ExternalAddress = new(
        @"(?:https?:|data:|blob:|javascript:|file:|//)|\u0000",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex EmbeddedImageKey = new(
        "\"(?:w|url|id|imageUrl|href|src|clickUrl)\"\\s*:",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // Room ad images may only come from the hotel's own room ads folder (same origin, one plain file name).
    public const string RoomAdImageFolder = "/nitro-assets/roomads/";

    private static readonly Regex RoomAdImage = new(
        @"^/nitro-assets/roomads/[A-Za-z0-9][A-Za-z0-9_-]{0,63}\.(?:png|gif|jpe?g|webp)\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsRoomAdImage(string value) => value != null && RoomAdImage.IsMatch(value);

    /// <summary>
    /// Branding accepts one exact imageUrl pair that is empty or names a file in <see cref="RoomAdImageFolder"/>;
    /// every other pair keeps the client image rules, so outside addresses and click links stay refused.
    /// </summary>
    public static bool RejectsBranding(IReadOnlyList<string> values)
    {
        if (values.Count % 2 != 0) {
            return true;
        }

        var rest = new List<string>(values.Count);
        var imageSeen = false;

        for (var index = 0; index < values.Count; index += 2) {
            var key = values[index] ?? "";
            var value = values[index + 1] ?? "";

            if (key == "imageUrl" && !imageSeen && (value.Length == 0 || IsRoomAdImage(value))) {
                imageSeen = true;
                continue;
            }

            rest.Add(key);
            rest.Add(value);
        }

        return RejectsClientImage(rest);
    }

    public static bool RejectsClientImage(IReadOnlyList<string> values)
    {
        for (var index = 0; index < values.Count; index++) {
            var value = values[index] ?? "";

            if (index % 2 == 0 && ClientImageKeys.Contains(value)) {
                return true;
            }

            if (ExternalAddress.IsMatch(value) || EmbeddedImageKey.IsMatch(value)) {
                return true;
            }
        }

        // Branding is decoded by MapDataFormat.Store. Validate exactly that representation
        // as well, including escaped keys, values and injected pair separators.
        var decoded = new MapDataFormat();

        try {
            decoded.Store(Branding(values));
        }
        catch (ArgumentException) {
            return true;
        }

        return decoded.Data.Any(pair => ClientImageKeys.Contains(pair.Key)
            || ExternalAddress.IsMatch(pair.Key) || ExternalAddress.IsMatch(pair.Value)
            || EmbeddedImageKey.IsMatch(pair.Key) || EmbeddedImageKey.IsMatch(pair.Value));
    }

    public static IFurniObjectData Load(ItemDefinition definition, string stored, bool keepLegacy)
    {
        stored ??= "";

        if (definition.Type == ItemType.Floor && HighscoreDataFormat.TryDefinition(definition.ItemName, out var scoreType, out var clearType)) {
            var board = new HighscoreDataFormat { ScoreType = scoreType, ClearType = clearType };

            // Legacy newly-created boards carry only their switch state; typed captured payloads never fall back to empty.
            if (stored is "" or "0" or "1") {
                board.State = stored;
            }
            else {
                board.Store(stored);

                if (board.ScoreType != scoreType || board.ClearType != clearType) {
                    throw new ArgumentException("Highscore types do not match the furniture definition.", nameof(stored));
                }
            }

            return board;
        }

        switch (definition.InteractionType) {
            case InteractionType.CrackableEgg: {
                    var data = definition.CreateData();

                    if (stored.Length > 0) {
                        data.Store(stored);
                    }

                    return data;
                }
            case InteractionType.AreaHide:
                return AreaHide.AreaHideState.Load(stored);
            case InteractionType.Background: {
                    var data = definition.CreateData();

                    if (stored.Length > 0) {
                        data.Store(BackgroundPairs(stored));
                    }

                    return data;
                }
            default:
                return keepLegacy ? new LegacyDataFormat { Data = stored } : FurniObjectData.Empty;
        }
    }

    public static string BackgroundPairs(string stored)
    {
        if (stored.Contains('\n')) {
            return stored;
        }

        var fields = stored.Split('\t');

        if (fields.Length < 2 || fields.Length % 2 != 0) {
            return stored;
        }

        return string.Join("\n", Enumerable.Range(0, fields.Length / 2).Select(index => $"{fields[index * 2]}\t{fields[index * 2 + 1]}"));
    }

    public static string Branding(IReadOnlyList<string> values)
    {
        var fields = new List<string> { "state", "0" };
        fields.AddRange(values);
        var pairs = fields.Count / 2;

        return string.Join("\n", Enumerable.Range(0, pairs).Select(index => $"{fields[index * 2]}\t{fields[index * 2 + 1]}"));
    }
}
