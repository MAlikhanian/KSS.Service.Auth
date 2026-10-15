using KSS.Helper.Enum.Base;
using Enum = KSS.Helper.Enum;

namespace KSS.Service
{
    public class Assistant
    {
        public static (byte SystemId, byte TableId) GetSystemAndTableId<T>()
        {
            return typeof(T).Name switch
            {
                _ => throw new ArgumentException("Unknown type"),
            };
        }
    }
}
