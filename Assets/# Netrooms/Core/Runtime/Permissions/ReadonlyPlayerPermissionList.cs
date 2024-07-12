using System.Collections.Generic;

namespace PixelsHub.Netrooms
{
    public class ReadonlyPlayerPermissionList
    {
        public readonly IReadOnlyList<ReadonlyPlayerPermission> list;

        public ReadonlyPlayerPermissionList(Dictionary<string, List<string>> register)
        {
            var list = new List<ReadonlyPlayerPermission>(register.Count);

            foreach(var keyValue in register)
                list.Add(new(keyValue.Key, keyValue.Value));

            this.list = list;
        }

        public static implicit operator ReadonlyPlayerPermissionList(Dictionary<string, List<string>> r) => new(r);
    }
}
