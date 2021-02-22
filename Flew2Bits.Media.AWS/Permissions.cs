using OrchardCore.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flew2Bits.Media.AWS
{
    public class Permissions: IPermissionProvider
    {
        public static readonly Permission ViewAWSMediaOptions = new Permission(nameof(ViewAWSMediaOptions), "View AWS Media Options");
        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ViewAWSMediaOptions }
                }
            };
        }

        public Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            return Task.FromResult(new[] { ViewAWSMediaOptions }.AsEnumerable());
        }
    }
}
