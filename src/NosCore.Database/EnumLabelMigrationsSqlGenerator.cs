//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace NosCore.Database
{
    // PostgreSQL cannot drop an enum label, so a rollback leaves the labels a migration added in
    // place, and adding one is skipped when it is already there. Columns keep their values either way.
    // Creating and dropping a whole enum type is left to the provider.
#pragma warning disable EF1001 // The provider's generator takes its options through an internal interface.
    public class EnumLabelMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies,
            INpgsqlSingletonOptions npgsqlSingletonOptions, IQuerySqlGeneratorFactory querySqlGeneratorFactory)
        : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions, querySqlGeneratorFactory)
#pragma warning restore EF1001
    {
        private const string EnumAnnotationPrefix = "Npgsql:Enum:";

        protected override void Generate(AlterDatabaseOperation operation, IModel? model, MigrationCommandListBuilder builder)
        {
            var additions = new List<(string Type, string Label, string? After, string? Before)>();
            foreach (var annotation in operation.GetAnnotations().Where(a => a.Name.StartsWith(EnumAnnotationPrefix)).ToList())
            {
                if (annotation.Value is not string target
                    || operation.OldDatabase.FindAnnotation(annotation.Name)?.Value is not string current
                    || target == current)
                {
                    continue;
                }

                var targetLabels = target.Split(',');
                var currentLabels = current.Split(',');
                for (var i = 0; i < targetLabels.Length; i++)
                {
                    if (currentLabels.Contains(targetLabels[i]))
                    {
                        continue;
                    }

                    additions.Add((annotation.Name[EnumAnnotationPrefix.Length..], targetLabels[i],
                        i > 0 ? targetLabels[i - 1] : null,
                        i == 0 && targetLabels.Length > 1 ? targetLabels[1] : null));
                }

                operation[annotation.Name] = current;
            }

            base.Generate(operation, model, builder);

            foreach (var (type, label, after, before) in additions)
            {
                var name = type.Split('.');
                var position = after != null ? $" AFTER {Literal(after)}" : before != null ? $" BEFORE {Literal(before)}" : "";
                builder
                    .Append($"ALTER TYPE {Dependencies.SqlGenerationHelper.DelimitIdentifier(name[^1], name.Length > 1 ? name[0] : null)}")
                    .Append($" ADD VALUE IF NOT EXISTS {Literal(label)}{position}")
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                    .EndCommand();
            }
        }

        private static string Literal(string value) => $"'{value.Replace("'", "''")}'";
    }
}
