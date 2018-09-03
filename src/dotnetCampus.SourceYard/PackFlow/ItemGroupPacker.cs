﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using dotnetCampus.SourceYard.Context;

namespace dotnetCampus.SourceYard.PackFlow
{
    internal class ItemGroupPacker : IPackFlow
    {
        /// <summary>
        /// 获取应该被加入源码引用的项类型。
        /// </summary>
        private static readonly string[] IncludingItemTypes =
        {
            // 由于需要多个项之间可能存在重合（用于 Update 和 Remove），所以 None 也是需要加入的。
            "Compile", "Resource", "Content", "None"
        };

        /// <summary>
        /// 获取应该被加入源码引用的 XAML 型的项类型。
        /// </summary>
        private static readonly string[] XamlItemTypes =
        {
            // 由于需要多个项之间可能存在重合（用于 Update 和 Remove），所以 None 也是需要加入的。
            "Page", "ApplicationDefinition"
        };

        public void Pack(IPackingContext context)
        {
            var buildAssetsFile = Path.Combine(context.PackingFolder, "build", $"{context.PackageId}.targets");

            // 从原始的项目文件中提取所有的 ItemGroup 中的节点，且节点类型在 IncludingItemTypes 中。

            // nuget 的源代码
            var sourceReferenceSourceFolder = @"$(MSBuildThisFileDirectory)..\src\";

            var (itemGroupElement, itemGroupElementOfXaml) = GetItemGroup(context.PackagedProjectFile, context.ProjectFolder);

            var itemGroup = ItemGroupToString(itemGroupElement, itemGroupElementOfXaml);

            // 读取文件
            var buildfile = File.ReadAllText(buildAssetsFile);
            var replace = "<!--替换ItemGroup-->";


            // 替换为 nuget 源代码的文件
            buildfile = buildfile.Replace(replace, itemGroup.Replace(SourceFile, sourceReferenceSourceFolder));

            // 本地的代码，用于调试本地的代码
            sourceReferenceSourceFolder = $@"$({context.PackageGuid}SourceFolder)\";
            replace = "<!--替换 SOURCE_REFERENCE ItemGroup-->";
            buildfile = buildfile.Replace(replace, itemGroup.Replace(SourceFile, sourceReferenceSourceFolder));

            // 用户可以选择使用 nuget 源代码，也可以选择使用自己的代码，所以就需要使用两个不同的值

            // 写入文件
            File.WriteAllText(buildAssetsFile, buildfile);
        }

        private string ItemGroupToString(XElement itemGroupElement, XElement itemGroupElementOfXaml)
        {
            return itemGroupElement.ToString() + "\r\n\r\n\r\n" + itemGroupElementOfXaml.ToString();
        }

        public (XElement itemGroupElement, XElement itemGroupElementOfXaml) GetItemGroup(PackagedProjectFile contextPackagedProjectFile, string projectFolder)
        {
            var compileFileList = GetFileList(contextPackagedProjectFile.CompileFile);
            var contentFileList = GetFileList(contextPackagedProjectFile.ContentFile);
            var resourceFileList = GetFileList(contextPackagedProjectFile.ResourceFile);
            var noneFileList = GetFileList(contextPackagedProjectFile.NoneFile);
            var embeddedResource = GetFileList(contextPackagedProjectFile.EmbeddedResource);
            var pageFileList = GetFileList(contextPackagedProjectFile.Page);

            var elementList = new List<XElement>();
            elementList.AddRange(IncludingItemCompileFileToElement(compileFileList, "Compile", false));
            elementList.AddRange(IncludingItemCompileFileToElement(contentFileList, "Resource", true));
            elementList.AddRange(IncludingItemCompileFileToElement(resourceFileList, "Content", true));
            elementList.AddRange(IncludingItemCompileFileToElement(embeddedResource, "EmbeddedResource", true));
            elementList.AddRange(IncludingItemCompileFileToElement(noneFileList, "None", true));

            var itemGroupElement = new XElement("ItemGroup", elementList);

            elementList = new List<XElement>();
            elementList.AddRange(XamlItemCompileFileToElement(pageFileList, "Page", false));

            var itemGroupElementOfXaml = new XElement("ItemGroup", elementList);

            return (itemGroupElement, itemGroupElementOfXaml);
        }

        private List<XElement> XamlItemCompileFileToElement(List<string> compileFileList, string includingItemTypes,
            bool copyToOutputDirectory)
        {
            var elementList = new List<XElement>();

            foreach (var temp in compileFileList)
            {
                var element = new XElement(includingItemTypes);

                element.SetAttributeValue("Include", SourceFile + temp);
                element.SetAttributeValue("SubType", "Designer");
                element.SetAttributeValue("Generator", "MSBuild:Compile");
                element.SetAttributeValue("Visible", "False");
                if (copyToOutputDirectory)
                {
                    element.SetAttributeValue("CopyToOutputDirectory", "PreserveNewest");
                }

                elementList.Add(element);
            }

            return elementList;
        }

        private List<XElement> IncludingItemCompileFileToElement(List<string> compileFileList,
            string includingItemTypes, bool copyToOutputDirectory)
        {
            var elementList = new List<XElement>();
            foreach (var temp in compileFileList)
            {
                var element = new XElement(includingItemTypes);
                element.SetAttributeValue("Include", SourceFile + temp);
                element.SetAttributeValue("Visible", "False");
                if (copyToOutputDirectory)
                {
                    element.SetAttributeValue("CopyToOutputDirectory", "PreserveNewest");
                }

                elementList.Add(element);
            }

            return elementList;
        }

        /// <summary>
        /// 用于替换的字符
        /// </summary>
        private const string SourceFile = @"-- replace folder --";

        private List<string> GetFileList(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                return new List<string>();
            }

            var fileList = File.ReadAllLines(file).ToList();

            fileList = RemoveTempFile(fileList);

            return fileList;
        }

        private List<string> RemoveTempFile(List<string> fileList)
        {
            fileList.RemoveAll
            (
                temp => temp.StartsWith("obj\\")
                        || temp.StartsWith("bin\\")
            );

            fileList.RemoveAll(temp =>
            {
                var pathRoot = Path.GetPathRoot(temp);
                if (!string.IsNullOrEmpty(pathRoot))
                {
                    return temp.StartsWith(pathRoot);
                }

                return false;
            });

            return fileList;
        }


        private static XElement ConvertItemElement(XElement item)
        {
            RemoveAllNamespaces(item);
            foreach (var attribute in new[]
            {
                item.Attribute("Include"),
                item.Attribute("Update"),
                item.Attribute("Remove")
            }.Where(x => x != null))
            {
                var path = attribute.Value;
                attribute.SetValue($@"$(MSBuildThisFileDirectory)..\src\{path}");
            }

            item.SetAttributeValue("Visible", "False");

            return item;
        }

        private static void RemoveAllNamespaces(XElement element)
        {
            element.Attributes().Where(e => e.IsNamespaceDeclaration).Remove();
            element.Name = element.Name.LocalName;

            foreach (var node in element.DescendantNodes())
            {
                if (node is XElement xElement)
                {
                    RemoveAllNamespaces(xElement);
                }
            }
        }

        [Pure]
        private static List<XPathNavigator> ReadProjectItems(string projectFile)
        {
            List<XPathNavigator> itemGroup;

            using (var fileStream = new FileInfo(projectFile).OpenRead())
            {
                var document = new XPathDocument(fileStream);
                var navigator = document.CreateNavigator();
                itemGroup = navigator.Select("/Project/ItemGroup").OfType<XPathNavigator>()
                    .SelectMany(x => x.SelectChildren(XPathNodeType.Element).OfType<XPathNavigator>()).ToList();
                if (itemGroup.Count <= 0)
                {
                    navigator.MoveToNamespace("");
                    var @namespace = new XmlNamespaceManager(new NameTable());
                    @namespace.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");
                    itemGroup = navigator.Select("/x:Project/x:ItemGroup", @namespace).OfType<XPathNavigator>()
                        .SelectMany(x => x.SelectChildren(XPathNodeType.Element).OfType<XPathNavigator>()).ToList();
                }
            }

            return itemGroup;
        }

        /// <summary>
        /// 读取 csproj/props/targets 文件，然后返回 Project 根节点。
        /// </summary>
        [Pure]
        private static (XElement root, XElement item, XElement xaml) ReadOrCreatePropsFile(string propsFile,
            string packageGuid, ILogger log)
        {
            var extension = Path.GetExtension(propsFile);

            if (!File.Exists(propsFile))
            {
                if (extension == ".props")
                {
                    var root = new XElement("Project");
                    return (root, root, root);
                }

                if (extension == ".targets")
                {
                    var target = new XElement("Target",
                        new XAttribute("Name", $"_{packageGuid}IncludeSourceCodes"),
                        new XAttribute("BeforeTargets", "CoreCompile")
                    );
                    var xamlTarget = new XElement("Target",
                        new XAttribute("Name", $"_{packageGuid}IncludeXamlCodes"),
                        new XAttribute("BeforeTargets", "XamlPreCompile")
                    );
                    var root = new XElement("Project", target, xamlTarget);
                    return (root, target, xamlTarget);
                }

                throw new NotSupportedException("仅支持为 props 和 targets 文件添加编译文件。");
            }

            using (var stream = new FileInfo(propsFile).OpenRead())
            {
                var root = XElement.Load(stream);

                XElement item;
                XElement xaml;
                if (extension == ".props")
                {
                    item = root;
                    xaml = root;
                }
                else if (extension == ".targets")
                {
                    item = root.XPathSelectElement($"Target[@Name='_{packageGuid}IncludeSourceCodes']");
                    xaml = root.XPathSelectElement($"Target[@Name='_{packageGuid}IncludeXamlCodes']");
                    if (item == null || xaml == null)
                    {
                        log.Error($"{Path.GetFileName(propsFile)} 中需要有一个用于引入源码的 Targets");
                    }
                }
                else
                {
                    throw new NotSupportedException("仅支持为 props 和 targets 文件添加编译文件。");
                }

                return (root, item, xaml);
            }
        }
    }
}