using System.Collections.Generic;

namespace PixelsHub.Netrooms
{
    public class ReadonlyPlayerPermission
    {
        public readonly string permissionCode;
        public IReadOnlyList<string> playerValues;

        public ReadonlyPlayerPermission(string permissionCode, List<string> playerValues)
        {
            this.permissionCode = permissionCode;
            this.playerValues = playerValues;
        }
    }
}
