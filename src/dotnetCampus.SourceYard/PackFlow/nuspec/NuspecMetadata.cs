﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace dotnetCampus.SourceYard.PackFlow.Nuspec
{
    [Serializable]
    [XmlType(typeName: "metadata", Namespace = "")]
    public class NuspecMetadata
    {
        [XmlElement("description")]
        public string Description { set; get; }

        [XmlArray(elementName: "dependencies", Namespace = "")]
        [XmlArrayItem(elementName: "dependency")]
        public List<NuspecDependency> Dependencies { set; get; } = new List<NuspecDependency>();

        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("copyright")]
        public string Copyright { get; set; }

        [XmlElement("licenseUrl")]
        public string PackageLicenseUrl { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("projectUrl")]
        public string PackageProjectUrl { get; set; }

        [XmlElement("iconUrl")]
        public string PackageIconUrl { get; set; }

        [XmlElement("tags")]
        public string PackageTags { get; set; }

        [XmlElement("releaseNotes")]
        public string PackageReleaseNotes { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("authors")]
        public string Authors { get; set; }

        [XmlElement("owners")]
        public string Owner { get; set; }

        /// <summary>
        /// 通过这个属性可以在安装源代码包的时候默认选 private assets 这样就可以让安装源代码包的项目被引用的时候，引用的项目不需要再安装源代码包
        /// </summary>
        [XmlElement("developmentDependency")]
        public string DevelopmentDependency { get; set; } = "true";

        [XmlElement("repository")]
        public Repository Repository { set; get; }
    }

    [XmlType(typeName: "repository", Namespace = "")]
    public class Repository
    {
        [XmlAttribute(attributeName: "type")]
        public string Type { set; get; }

        [XmlAttribute(attributeName: "url")]
        public string Url { set; get; }
    }

    public class NugetTargetFramework
    {
        [XmlAttribute("targetFramework")]
        public string TargetFramework { get; set; } = ".NETFramework4.5";

        [XmlArray(elementName: "dependencies", Namespace = "")]
        [XmlArrayItem(elementName: "dependency")]
        public List<NuspecDependency> Dependencies { set; get; } = new List<NuspecDependency>();
    }
}