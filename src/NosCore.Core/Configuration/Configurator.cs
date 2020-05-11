using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;

namespace NosCore.Core.Configuration
{
    public static class Configurator
    {
        private static IConfiguration ReplaceEnvironment(IConfiguration configuration, object strongTypedConfiguration)
        {
            var ses = configuration.GetChildren().ToList();
            for (var index = ses.Count; index > 0; index--)
            {
                if (ses[index - 1].Value != null)
                {
                    var regexp = new Regex(@"\${(?<variable>[a-zA-Z_]+)\s*,?\s*(?<fallback>[^}]+)}");
                    var matches = regexp.Matches(ses[index - 1].Value);
                    foreach (var match in matches.ToList())
                    {
                        var value = Environment.GetEnvironmentVariable(match.Groups[1].Value);
                        if (string.IsNullOrEmpty(value) && (match.Groups.Count > 2) && (match.Groups[2].Value != null))
                        {
                            value ??= match.Groups[2].Value;
                        }
                        ses[index - 1].Value = regexp.Replace(ses[index - 1].Value, value!, 1);
                    }
                }
                else if (ses[index - 1].GetChildren().Any())
                {
                    ReplaceEnvironment(configuration.GetSection(ses[index - 1].Path), configuration).Bind(strongTypedConfiguration);
                }
            }

            return configuration;
        }
        public static void Configure(IConfiguration configuration, object strongTypedConfiguration)
        {
            if (configuration != null)
            {
                ReplaceEnvironment(configuration, strongTypedConfiguration).Bind(strongTypedConfiguration);
            }

            Validator.ValidateObject(strongTypedConfiguration, new ValidationContext(strongTypedConfiguration), true);
        }
    }
}
