using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace NosCore.Core.Configuration
{
    public static class Configurator
    {
        public static void Configure(IConfiguration configuration, object strongTypedConfiguration)
        {
            var regexp = new Regex(@"\${(?<variable>[a-zA-Z_]+)\s+,?\s+(?<fallback>[^}]+)}");
            if (configuration != null)
            {
                foreach (var s in configuration.GetChildren().Where(s => s.Value != null).ToList())
                {
                    var matches = regexp.Matches(s.Value);
                    foreach (var match in matches.ToList())
                    {
                        var value = Environment.GetEnvironmentVariable(match.Groups[1].Value);
                        if (value == null && (match.Groups.Count > 2) && (match.Groups[2].Value != null))
                        {
                            value ??= match.Groups[2].Value;
                        }

                        s.Value = regexp.Replace(s.Value, value!, 1);
                    }
                }

                configuration.Bind(strongTypedConfiguration);
            }

            Validator.ValidateObject(strongTypedConfiguration, new ValidationContext(strongTypedConfiguration), true);
        }
    }
}
