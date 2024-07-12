using System.Collections.Generic;

namespace PixelsHub.Netrooms
{
    public class ReadonlyPlayerPermissionList
    {
        public readonly IReadOnlyList<ReadonlyPlayerPermission> list;

        public ReadonlyPlayerPermissionList(PlayerPermissions.Register register)
        {
            var list = new List<ReadonlyPlayerPermission>(register.Count);

            foreach(var keyValue in register)
                list.Add(new(keyValue.Key, keyValue.Value));

            this.list = list;
        }

        public static implicit operator ReadonlyPlayerPermissionList(PlayerPermissions.Register r) => new(r);
    }

}
