namespace DsbNorge.A3Forms.Common;

public static class Extensions
{
    // fixing brain dead C# which does not include this for lists as for strings
    public static bool IsNullOrEmpty<T>(this List<T>? list)
    {
        return list == null || list.Count == 0;
    }
}