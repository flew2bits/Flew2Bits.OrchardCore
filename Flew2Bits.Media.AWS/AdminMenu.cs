using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using System;
using System.Threading.Tasks;

namespace Flew2Bits.Media.AWS
{
    public class AdminMenu : INavigationProvider
    {
        private readonly IStringLocalizer S;

        public AdminMenu(IStringLocalizer<AdminMenu> localizer)
        {
            S = localizer;
        }

        public Task BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!String.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            builder.Add(S["Configuration"], configuration => configuration
                .Add(S["Media"], S["Media"].PrefixPosition(), media => media
                    .Add(S["AWS S3 Options"], S["AWS S3 Options"].PrefixPosition(), options => options
                        .Action("Options", "Admin", new { area = "Flew2Bits.Media.AWS" })
                        .Permission(Permissions.ViewAWSMediaOptions)
                        .LocalNav())
            ));

            return Task.CompletedTask;
        }
    }
}
