//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NosCore.DtoGenerator
{
    [Generator]
    public class DtoGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //uncomment line to debug generator
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var path = Path.GetDirectoryName(context.AdditionalFiles.First(f => f.Path.EndsWith(".csproj")).Path);
            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            var options = (context.Compilation as CSharpCompilation)?.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions
                ?? new CSharpParseOptions(LanguageVersion.Preview);
            var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), options)).ToList();
            var compilation = context.Compilation.AddSyntaxTrees(trees);
            foreach (var ns in trees)
            {
                var semanticModel = compilation.GetSemanticModel(ns);
                foreach (var cc in ns.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var className = cc.Identifier.ValueText;
                    var fullEntityName = (cc.Parent as NamespaceDeclarationSyntax)?.Name.ToString();
                    if (fullEntityName != "NosCore.Database.Entities")
                    {
                        continue;
                    }
                    //var directory 
                    string classNamespace = "NosCore.Data.Dto";
                    int keyCount = 0;
                    string dtoInterface = "IDto";
                    var propertyList = "";
                    var classSymbol = cc.BaseList?.Types.Select(s => semanticModel.GetTypeInfo(s.Type).Type!);
                    if (classSymbol?.Any(s => s.TypeKind != TypeKind.Interface) == true)
                    {
                        if (classSymbol.Any(s => s.Name.EndsWith("Instance")))
                        {
                            dtoInterface = classSymbol.First(s => s.Name.EndsWith("Instance")).Name + "Dto";
                            keyCount = 1;
                        }
                        else
                        {
                            foreach (var symbol in classSymbol.First().GetMembers()
                                .Where(m => m.Kind == SymbolKind.Property))
                            {
                                var prop = (IPropertySymbol)symbol;
                                var keyAttribute = prop.GetAttributes().FirstOrDefault(o => o.AttributeClass?.Name == "KeyAttribute");
                                if (keyAttribute != null)
                                {
                                    keyCount++;
                                    propertyList += "       [Key]\n";
                                }
                                propertyList += $"       public {prop.Type} {prop.Name} {{get; set;}}\n\n";
                            }
                        }
                    }
                    if (className == "ItemInstance")
                    {
                        dtoInterface = "IItemInstanceDto";
                    }
                    if (className.StartsWith("I18N"))
                    {
                        //directory = "I18N";
                        dtoInterface = "II18NDto";
                        classNamespace = "NosCore.Data.I18N";
                    }

                    if (classSymbol?.FirstOrDefault(s => s.TypeKind == TypeKind.Interface && s.Name.Contains("IStaticEntity")) != null)
                    {
                        //directory = "StaticEntities";
                        dtoInterface = "IStaticDto";
                        classNamespace = "NosCore.Data.StaticEntities";
                    }

                    var props = cc.DescendantNodes().OfType<PropertyDeclarationSyntax>();

                    foreach (var prop in props)
                    {
                        var protectionLevel = "public";
                        var typeSymbol = semanticModel.GetTypeInfo(prop.Type).Type;

                        var i18nStr = prop.AttributeLists.SelectMany(o => o.Attributes).FirstOrDefault(o => o.Name.ToString() == "I18NString");
                        if (i18nStr != null)
                        {
                            propertyList += $"       [I18NFrom({i18nStr.ArgumentList!.Arguments[0].ToString().TrimEnd(')')}Dto))]";
                            propertyList += $"       public I18NString {prop.Identifier} {{ get; set; }} = new I18NString();";
                            propertyList += $"       [AdaptMember(\"{prop.Identifier}\")]";
                            propertyList += $"       public {prop.Type} {prop.Identifier}I18NKey {{ get; set; }}";
                        }
                        else
                        {

                            var nullableType = prop.Type.ToString().Contains("?");
                            if (nullableType)
                            {
                                propertyList += "#nullable enable\n";
                            }

                            var namespaceType = typeSymbol?.ContainingNamespace == null ? "" : typeSymbol.ContainingNamespace + ".";
                            if (typeSymbol?.ContainingType != null)
                            {
                                namespaceType = typeSymbol?.ContainingType + ".";
                            }

                            var dtoName = prop.Type.ToString();
                            if ((typeSymbol as INamedTypeSymbol)?.TypeArguments.Any() == true && typeSymbol?.Name == "Nullable" && (typeSymbol as INamedTypeSymbol)!.TypeArguments[0].ToString().StartsWith("NosCore"))
                            {
                                namespaceType = "";
                                dtoName = (typeSymbol as INamedTypeSymbol)!.TypeArguments[0].ToString();
                                if (nullableType)
                                {
                                    dtoName += "?";
                                }
                            }

                            if (prop.Modifiers.Any(s => s.ValueText == "virtual"))
                            {
                                protectionLevel = "internal";
                                if (!dtoName.EndsWith(">"))
                                {
                                    namespaceType = "";
                                }

                                if (nullableType)
                                {
                                    dtoName = dtoName.Replace("?", "");
                                }
                                dtoName = dtoName.Insert(dtoName.EndsWith(">") ? dtoName.Length - 1 : dtoName.Length, "Dto");
                                if (nullableType)
                                {
                                    dtoName += "?";
                                }
                            }

                            if (namespaceType == "System.")
                            {
                                namespaceType = "";
                            }
                            var keyAttribute = prop.AttributeLists.SelectMany(o => o.Attributes).FirstOrDefault(o => o.Name.ToString() == "Key");
                            if (keyAttribute != null)
                            {
                                keyCount++;
                                propertyList += "       [Key]\n";
                            }

                            propertyList += $"       {protectionLevel} {namespaceType}{dtoName} {prop.Identifier} {{get; set;}}\n";
                            if (nullableType)
                            {
                                propertyList += "#nullable disable\n";
                            }
                        }

                        propertyList += "\n";
                    }

                    if (keyCount != 1)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SG0001",
                                "Missing primary key",
                                $"Class {className} does not have a key.",
                                "SourceGenerator",
                                DiagnosticSeverity.Error,
                                true), null));
                    }
                
                    var code = $@"//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.ComponentModel.DataAnnotations;
using NosCore.Data.I18N;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Data.DataAttributes;
using NosCore.Data.Enumerations.I18N;
using NodaTime;
using Mapster;
using System;

namespace {classNamespace}
{{
    /// <summary>
    /// Represents a DTO class for {fullEntityName}.
    /// NOTE: This class is generated by NosCore.DtoGenerator
    /// </summary>
	public class {className}Dto : {dtoInterface}
    {{               
{propertyList}              
    }}
}}";
                    context.AddSource(
                        $"{className}Dto.generated.cs",
                        SourceText.From(code, Encoding.UTF8)
                    );
                }
            }
        }
    }
}
