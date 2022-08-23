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

using System;

namespace SpoiledCat.Utilities
{
	using System.Linq;

	public static class MonoPosixShim
	{
		private static System.Reflection.Assembly monoPosixAsm;
		private static bool? hasMonoPosix;

		private static Func<int, int> monoUnixNativeSyscall_close = null;

		private static Func<string, int, int, int> monoUnixNativeSyscall_open = null;

		private static Func<string, int, int> monoUnixNativeSyscall_chmod = null;

		/// <summary>
		///
		/// </summary>
		/// <param name="pathname"></param>
		/// <param name="openFlags">Mono.Unix.Native.OpenFlags</param>
		/// <param name="filePermissions">Mono.Unix.Native.FilePermissions</param>
		/// <returns></returns>
		public static int Open(string pathname, int openFlags, int filePermissions) => MonoUnixNativeSyscall_open(pathname, openFlags, filePermissions);

		public static int Close(int fd) => MonoUnixNativeSyscall_close(fd);
		public static int Chmod(string file, int filePermissions) => MonoUnixNativeSyscall_chmod(file, filePermissions);

		public static bool HasMonoPosix
		{
			get
			{
				if (!hasMonoPosix.HasValue)
				{
					monoPosixAsm = AppDomain.CurrentDomain.GetAssemblies()
									.FirstOrDefault(x => x.FullName.StartsWith("Mono.Posix"));
					hasMonoPosix = monoPosixAsm != null;
				}
				return hasMonoPosix.Value;
			}
		}

		public static Func<int, int> MonoUnixNativeSyscall_close
		{
			get
			{
				if (monoUnixNativeSyscall_close == null && HasMonoPosix)
				{
					var type = monoPosixAsm.GetType("Mono.Unix.Native.Syscall");
					if (type != null)
					{
						var method = type.GetMethod("close", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
						if (method != null)
						{
							monoUnixNativeSyscall_close = (p) => {
								return (int)method.Invoke(null, new object[] { p });
							};
						}
					}

					if (monoUnixNativeSyscall_close == null)
						monoUnixNativeSyscall_close = p => -1;
				}
				return monoUnixNativeSyscall_close;
			}
		}

		private static Func<string, int, int, int> MonoUnixNativeSyscall_open
		{
			get
			{
				if (monoUnixNativeSyscall_open == null && HasMonoPosix)
				{
					var type = monoPosixAsm.GetType("Mono.Unix.Native.Syscall");
					if (type != null)
					{
						var method = type.GetMethod("open", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new[] { typeof(string), typeof(int), typeof(int) }, null);
						if (method != null)
						{
							monoUnixNativeSyscall_open = (pathname, flags, mode) => {
								return (int)method.Invoke(null, new object[] { pathname, flags, mode });
							};
						}
					}

					if (monoUnixNativeSyscall_open == null)
						monoUnixNativeSyscall_open = (_, __, ___) => -1;
				}
				return monoUnixNativeSyscall_open;
			}
		}

		private static Func<string, int, int> MonoUnixNativeSyscall_chmod
		{
			get
			{
				if (monoUnixNativeSyscall_chmod == null && HasMonoPosix)
				{
					var type = monoPosixAsm.GetType("Mono.Unix.Native.Syscall");
					if (type != null)
					{
						var method = type.GetMethod("chmod", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new[] { typeof(string), typeof(int) }, null);
						if (method != null)
						{
							monoUnixNativeSyscall_chmod = (pathname, mode) => {
								return (int)method.Invoke(null, new object[] { pathname, mode });
							};
						}
					}

					if (monoUnixNativeSyscall_chmod == null)
						monoUnixNativeSyscall_chmod = (_, __) => -1;
				}
				return monoUnixNativeSyscall_chmod;
			}
		}
	}
}

