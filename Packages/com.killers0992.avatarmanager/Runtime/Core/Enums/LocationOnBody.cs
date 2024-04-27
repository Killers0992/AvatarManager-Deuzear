using System;

namespace AvatarManager.Core
{
    [Flags]
    public enum LocationOnBody
    {
        Unknown = 0,
        Head = 1,
        Face = 2,
        Arms = 4,
        Back = 8,
        LowerBack = 16,
        Chest = 32,
        Hips = 64,
        Legs = 128,
        Knees = 256,
        Feets = 512,
        Tail = 1024
    }
}