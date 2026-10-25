namespace Plus.HabboHotel.Rooms;

/// <summary>
/// A tile in a room an avatar stood on, used to put a returning user back where they left.
/// </summary>
public readonly record struct RoomEntrySpot(uint RoomId, int X, int Y, double Z, int Rotation);
