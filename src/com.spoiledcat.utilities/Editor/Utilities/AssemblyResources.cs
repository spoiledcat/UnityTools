// Copyright 2016-2022 Andreia Gaita
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if SPOILEDCAT_HAS_IO
using System.IO;
using System.Linq;
using System.Reflection;

namespace SpoiledCat.Utilities
{
	using SimpleIO;
	using Extensions;
	using Unity;

	public enum ResourceType
	{
		Icon,
		Platform,
		Generic
	}

	public class AssemblyResources
	{
		public static SPath ToFile(ResourceType resourceType, string resource, SPath destinationPath, IEnvironment environment)
		{
			var target = destinationPath.Combine(resource);
			var source = TryGetFile(resourceType, resource, environment);
			if (source.IsInitialized)
			{
				target.DeleteIfExists();
				return source.Copy(target);
			}
			return SPath.Default;
		}

		public static Stream ToStream(ResourceType resourceType, string resource, IEnvironment environment)
		{
			return TryGetStream(resourceType, resource, environment);
		}

		private static (string type, string os) ParseResourceType(ResourceType resourceType, IEnvironment environment)
		{
			var os = "";
			if (resourceType == ResourceType.Platform)
			{
				os =  environment.IsWindows ? "windows"
					: environment.IsLinux ? "linux"
					: "mac";
			}
			var type = resourceType == ResourceType.Icon ? "IconsAndLogos"
				: resourceType == ResourceType.Platform ? "PlatformResources"
				: "Resources";

			return (type, os);
		}

		private static Stream TryGetResource(ResourceType resourceType, string type, string os, string resource)
		{
			// all the resources are embedded in Git.Api
			var asm = Assembly.GetCallingAssembly();
			if (resourceType != ResourceType.Icon)
				asm = typeof(AssemblyResources).Assembly;

			var resourceName =
				$"{type}{(!string.IsNullOrEmpty(os) ? "." + os : os)}.{resource}";
			resourceName = asm.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceName));
			return resourceName != null ? asm.GetManifestResourceStream(resourceName) : null;
		}

		private static Stream TryGetStream(ResourceType resourceType, string resource, IEnvironment environment)
		{
			/*
				This function attempts to get files embedded in the callers assembly.
				Unity.VersionControl.Git which tends to contain logos
				Git.Api which tends to contain application resources

				Each file's name is their physical path in the project.

				When running tests, we assume the tests are looking for application resources, and default to returning Git.Api

				First check for the resource in the calling assembly.
				If the resource cannot be found, fallback to looking in Git.Api's assembly.
				If the resource is still not found, it attempts to find it in the file system
			 */

			(string type, string os) = ParseResourceType(resourceType, environment);

			var stream = TryGetResource(resourceType, type, os, resource);
			if (stream != null)
				return stream;

			SPath possiblePath = environment.ExtensionInstallPath.Combine(type, os, resource);
			if (possiblePath.FileExists())
			{
				return new MemoryStream(possiblePath.ReadAllBytes());
			}
			return null;
		}

		private static SPath TryGetFile(ResourceType resourceType, string resource, IEnvironment environment)
		{
			/*
				This function attempts to get files embedded in the callers assembly.

				Each file's name is their physical path in the project.

				First check for the resource in the calling assembly.
				If the resource is still not found, it attempts to find it in the file system
			 */

			(string type, string os) = ParseResourceType(resourceType, environment);

			var stream = TryGetResource(resourceType, type, os, resource);
			if (stream != null)
			{
				var target = SPath.GetTempFilename();
				return target.WriteAllBytes(stream.ToByteArray());
			}

			SPath possiblePath = environment.ExtensionInstallPath.Combine(type, os, resource);
			if (possiblePath.FileExists())
			{
				return possiblePath;
			}

			return SPath.Default;
		}
	}
}
#endif
