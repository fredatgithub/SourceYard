﻿using System;
using System.IO;
using System.IO.Compression;
using dotnetCampus.SourceYard.Context;

namespace dotnetCampus.SourceYard.PackFlow
{
    internal class NuGetPacker : IPackFlow
    {
        public void Pack(IPackingContext context)
        {
            var targetPackageFile = Path.GetFullPath(Path.Combine(context.PackageOutputPath,
                $"{context.PackageId}.{context.PackageVersion}.nupkg"));
            if (File.Exists(targetPackageFile))
            {
                File.Delete(targetPackageFile);
            }

            var directory = Path.GetDirectoryName(targetPackageFile);
            if (directory == null)
            {
                throw new NotSupportedException("不支持相对路径。");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ZipFile.CreateFromDirectory(context.PackingFolder, targetPackageFile);
        }
    }
}