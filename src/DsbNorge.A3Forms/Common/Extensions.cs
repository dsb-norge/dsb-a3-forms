namespace DsbNorge.A3Forms.Common;

public static class Extensions
{
    // fixing brain-dead C# which does not include this for lists and strings
    public static bool IsNullOrEmpty(this string value)
    {
        return string.IsNullOrEmpty(value);
    }
    
    public static bool IsNullOrEmpty<T>(this List<T>? list)
    {
        return list == null || list.Count == 0;
    }
}