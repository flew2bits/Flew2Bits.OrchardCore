using OrchardCore.Data.Migration;
using OrchardCore.Recipes.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Flew2Bits.HtmlMenu
{
    public class Migrations: DataMigration
    {
        private readonly IRecipeMigrator _recipeMigrator;

        public Migrations(IRecipeMigrator recipeMigrator)
        {
            _recipeMigrator = recipeMigrator;
        }

        public async Task<int> CreateAsync()
        {
            await _recipeMigrator.ExecuteAsync("htmlmenu.recipe.json", this);
            return 1;
        }
    }
}
