﻿//-----------------------------------------------------------------------
// <license file="SimpleIO.cs">
//
// The MIT License(MIT)
// =====================
//
// Copyright © `2017-2019` `Andreia Gaita`
// Copyright © `2015-2017` `Lucas Meijer`
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// </license>
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.opensource.org/licenses/mit-license.php
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SpoiledCat.SimpleIO
{
	[Serializable]
	[DebuggerDisplay("{DebuggerDisplay,nq}")]

	public struct SPath : IEquatable<SPath>, IComparable
	{
		public static SPath Default;

		private readonly string[] _elements;
		private readonly string _driveLetter;

		#region construction

		public SPath(string path)
		{
			if (path == null)
				throw new ArgumentNullException("path");

			IsInitialized = true;

			path = ParseDriveLetter(path, out _driveLetter);

			if (path == "/")
			{
				IsRelative = false;
				_elements = new string[] {};
			}
			else
			{
				var split = path.Split('/', '\\');

				IsRelative = _driveLetter == null && IsRelativeFromSplitString(split);

				_elements = ParseSplitStringIntoElements(split.Where(s => s.Length > 0).ToArray(), IsRelative);
			}
		}

		public static (SPath, bool) TryParse(string path)
		{
			if (path == null) return (SPath.Default, false);
			var p = new SPath(path);
			return (p, !p.IsEmpty || p.IsRoot);
		}

		private SPath(string[] elements, bool isRelative, string driveLetter)
		{
			_elements = elements;
			IsRelative = isRelative;
			_driveLetter = driveLetter;
			IsInitialized = true;
		}

		private static string[] ParseSplitStringIntoElements(IEnumerable<string> inputs, bool isRelative)
		{
			var stack = new List<string>();

			foreach (var input in inputs.Where(input => input.Length != 0))
			{
				if (input == ".")
				{
					if ((stack.Count > 0) && (stack.Last() != "."))
						continue;
				}
				else if (input == "..")
				{
					if (HasNonDotDotLastElement(stack))
					{
						stack.RemoveAt(stack.Count - 1);
						continue;
					}
					if (!isRelative)
						throw new ArgumentException("You cannot create a path that tries to .. past the root");
				}
				stack.Add(input);
			}
			return stack.ToArray();
		}

		private static bool HasNonDotDotLastElement(List<string> stack)
		{
			return stack.Count > 0 && stack[stack.Count - 1] != "..";
		}

		private static string ParseDriveLetter(string path, out string driveLetter)
		{
			if (path.Length >= 3 && path[1] == ':' && (path[2] == '/' || path[2] == '\\'))
			{
				driveLetter = path[0].ToString();
				return path.Substring(2);
			}

			driveLetter = null;
			return path;
		}

		private static bool IsRelativeFromSplitString(string[] split)
		{
			if (split.Length < 2)
				return true;

			return split[0].Length != 0 || !split.Any(s => s.Length > 0);
		}

		public SPath Combine(params string[] append)
		{
			return Combine(append.Select(a => new SPath(a)).ToArray());
		}

		public SPath Combine(params SPath[] append)
		{
			ThrowIfNotInitialized();

			if (!append.All(p => p.IsRelative))
				throw new ArgumentException("You cannot .Combine a non-relative path");

			return new SPath(
				ParseSplitStringIntoElements(_elements.Concat(append.SelectMany(p => p._elements)), IsRelative),
				IsRelative, _driveLetter);
		}

		public SPath Parent {
			get {
				ThrowIfNotInitialized();

				if (_elements.Length == 0)
					throw new InvalidOperationException("Parent is called on an empty path");

				var newElements = _elements.Take(_elements.Length - 1).ToArray();

				return new SPath(newElements, IsRelative, _driveLetter);
			}
		}

		public SPath RelativeTo(SPath path)
		{
			ThrowIfNotInitialized();

			if (!IsChildOf(path))
			{
				if (!IsRelative && !path.IsRelative && _driveLetter != path._driveLetter)
					throw new ArgumentException(
						"Path.RelativeTo() was invoked with two paths that are on different volumes. invoked on: " +
						ToString() + " asked to be made relative to: " + path);

				SPath commonParent = Default;
				foreach (var parent in RecursiveParents)
				{
					commonParent = path.RecursiveParents.FirstOrDefault(otherParent => otherParent == parent);

					if (commonParent.IsInitialized)
						break;
				}

				if (!commonParent.IsInitialized)
					throw new ArgumentException("Path.RelativeTo() was unable to find a common parent between " +
												ToString() + " and " + path);

				if (IsRelative && path.IsRelative && commonParent.IsEmpty)
					throw new ArgumentException(
						"Path.RelativeTo() was invoked with two relative paths that do not share a common parent.  Invoked on: " +
						ToString() + " asked to be made relative to: " + path);

				var depthDiff = path.Depth - commonParent.Depth;
				return new SPath(
					Enumerable.Repeat("..", depthDiff).Concat(_elements.Skip(commonParent.Depth)).ToArray(), true,
					null);
			}

			return new SPath(_elements.Skip(path._elements.Length).ToArray(), true, null);
		}

		public SPath GetCommonParent(SPath path)
		{
			ThrowIfNotInitialized();

			if (!IsChildOf(path))
			{
				if (!IsRelative && !path.IsRelative && _driveLetter != path._driveLetter)
					return Default;

				SPath commonParent = Default;
				foreach (var parent in new List<SPath> { this }.Concat(RecursiveParents))
				{
					commonParent = path.RecursiveParents.FirstOrDefault(otherParent => otherParent == parent);
					if (commonParent.IsInitialized)
						break;
				}

				if (IsRelative && path.IsRelative && (!commonParent.IsInitialized || commonParent.IsEmpty))
					return Default;
				return commonParent;
			}
			return path;
		}

		public SPath ChangeExtension(string extension)
		{
			ThrowIfNotInitialized();
			ThrowIfRoot();

			var newElements = (string[])_elements.Clone();
			newElements[newElements.Length - 1] =
				FileSystem.ChangeExtension(_elements[_elements.Length - 1], WithDot(extension));
			if (extension == string.Empty)
				newElements[newElements.Length - 1] = newElements[newElements.Length - 1].TrimEnd('.');
			return new SPath(newElements, IsRelative, _driveLetter);
		}

		#endregion construction

		#region inspection

		public bool IsRelative { get; }

		public string FileName {
			get {
				ThrowIfNotInitialized();
				ThrowIfRoot();

				return _elements.Last();
			}
		}

		public string FileNameWithoutExtension {
			get {
				ThrowIfNotInitialized();

				return FileSystem.GetFileNameWithoutExtension(FileName);
			}
		}

		public IEnumerable<string> Elements {
			get {
				ThrowIfNotInitialized();
				return _elements;
			}
		}

		public int Depth {
			get {
				ThrowIfNotInitialized();
				return _elements.Length;
			}
		}

		public bool IsInitialized { get; }

		public bool Exists()
		{
			ThrowIfNotInitialized();
			return FileExists() || DirectoryExists();
		}

		public bool Exists(string append)
		{
			ThrowIfNotInitialized();
			if (String.IsNullOrEmpty(append))
			{
				return Exists();
			}
			return Exists(new SPath(append));
		}

		public bool Exists(SPath append)
		{
			ThrowIfNotInitialized();
			if (!append.IsInitialized)
				return Exists();
			return FileExists(append) || DirectoryExists(append);
		}

		public bool DirectoryExists()
		{
			ThrowIfNotInitialized();
			return FileSystem.DirectoryExists(MakeAbsolute());
		}

		public bool DirectoryExists(string append)
		{
			ThrowIfNotInitialized();
			if (String.IsNullOrEmpty(append))
				return DirectoryExists();
			return DirectoryExists(new SPath(append));
		}

		public bool DirectoryExists(SPath append)
		{
			ThrowIfNotInitialized();
			if (!append.IsInitialized)
				return DirectoryExists();
			return FileSystem.DirectoryExists(Combine(append).MakeAbsolute());
		}

		public bool FileExists()
		{
			ThrowIfNotInitialized();
			return FileSystem.FileExists(MakeAbsolute());
		}

		public bool FileExists(string append)
		{
			ThrowIfNotInitialized();
			if (String.IsNullOrEmpty(append))
				return FileExists();
			return FileExists(new SPath(append));
		}

		public bool FileExists(SPath append)
		{
			ThrowIfNotInitialized();
			if (!append.IsInitialized)
				return FileExists();
			return FileSystem.FileExists(Combine(append).MakeAbsolute());
		}

		public string ExtensionWithDot {
			get {
				ThrowIfNotInitialized();
				if (IsRoot)
					throw new ArgumentException("A root directory does not have an extension");

				var last = _elements.Last();
				var index = last.LastIndexOf(".");
				if (index < 0) return String.Empty;
				return last.Substring(index);
			}
		}

		public string InQuotes()
		{
			return "\"" + ToString() + "\"";
		}

		public string InQuotes(SlashMode slashMode)
		{
			return "\"" + ToString(slashMode) + "\"";
		}

		public override string ToString()
		{
			return ToString(SlashMode.Native);
		}

		public string ToString(SlashMode slashMode)
		{
			if (!IsInitialized)
				return String.Empty;

			// Check if it's linux root /
			if (IsRoot && string.IsNullOrEmpty(_driveLetter))
				return Slash(slashMode).ToString();

			if (IsRelative && _elements.Length == 0)
				return ".";

			var sb = new StringBuilder();
			if (_driveLetter != null)
			{
				sb.Append(_driveLetter);
				sb.Append(":");
			}
			if (!IsRelative)
				sb.Append(Slash(slashMode));
			var first = true;
			foreach (var element in _elements)
			{
				if (!first)
					sb.Append(Slash(slashMode));

				sb.Append(element);
				first = false;
			}
			return sb.ToString();
		}

		public static implicit operator string(SPath path)
		{
			return path.ToString();
		}

		static char Slash(SlashMode slashMode)
		{
			switch (slashMode)
			{
				case SlashMode.Backward:
					return '\\';
				case SlashMode.Forward:
					return '/';
				default:
					return FileSystem.DirectorySeparatorChar;
			}
		}

		public override bool Equals(Object other)
		{
			if (other is SPath)
			{
				return Equals((SPath)other);
			}
			return false;
		}

		public bool Equals(SPath p)
		{
			if (p.IsInitialized != IsInitialized)
				return false;

			// return early if we're comparing two SPath.Default instances
			if (!IsInitialized)
				return true;

			if (p.IsRelative != IsRelative)
				return false;

			if (!string.Equals(p._driveLetter, _driveLetter, PathStringComparison))
				return false;

			if (p._elements.Length != _elements.Length)
				return false;

			for (var i = 0; i != _elements.Length; i++)
				if (!string.Equals(p._elements[i], _elements[i], PathStringComparison))
					return false;

			return true;
		}

		public static bool operator ==(SPath lhs, SPath rhs)
		{
			return lhs.Equals(rhs);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + IsInitialized.GetHashCode();
				if (!IsInitialized)
					return hash;
				hash = hash * 23 + IsRelative.GetHashCode();
				foreach (var element in _elements)
					hash = hash * 23 + (IsUnix ? element : element.ToUpperInvariant()).GetHashCode();
				if (_driveLetter != null)
					hash = hash * 23 + (IsUnix ? _driveLetter : _driveLetter.ToUpperInvariant()).GetHashCode();
				return hash;
			}
		}

		public int CompareTo(object other)
		{
			if (!(other is SPath))
				return -1;

			return ToString().CompareTo(((SPath)other).ToString());
		}

		public static bool operator !=(SPath lhs, SPath rhs)
		{
			return !(lhs.Equals(rhs));
		}

		public bool HasExtension(params string[] extensions)
		{
			ThrowIfNotInitialized();
			var extensionWithDotLower = ExtensionWithDot.ToLower();
			return extensions.Any(e => WithDot(e).ToLower() == extensionWithDotLower);
		}

		private static string WithDot(string extension)
		{
			return extension.StartsWith(".") ? extension : "." + extension;
		}

		public bool IsEmpty {
			get {
				ThrowIfNotInitialized();
				return _elements.Length == 0;
			}
		}

		public bool IsRoot {
			get {
				return IsEmpty && !IsRelative;
			}
		}

		#endregion inspection

		#region directory enumeration

		public IEnumerable<SPath> Files(string filter, bool recurse = false)
		{
			return FileSystem
				.GetFiles(MakeAbsolute(), filter,
					recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => new SPath(s));
		}

		public IEnumerable<SPath> Files(bool recurse = false)
		{
			return Files("*", recurse);
		}

		public IEnumerable<SPath> Contents(string filter, bool recurse = false)
		{
			return Files(filter, recurse).Concat(Directories(filter, recurse));
		}

		public IEnumerable<SPath> Contents(bool recurse = false)
		{
			return Contents("*", recurse);
		}

		public IEnumerable<SPath> Directories(string filter, bool recurse = false)
		{
			return FileSystem.GetDirectories(MakeAbsolute(), filter,
				recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select(s => new SPath(s));
		}

		public IEnumerable<SPath> Directories(bool recurse = false)
		{
			return Directories("*", recurse);
		}

		#endregion

		#region filesystem writing operations

		public SPath CreateFile()
		{
			ThrowIfNotInitialized();
			ThrowIfRelative();
			ThrowIfRoot();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllBytes(MakeAbsolute(), new byte[0]);
			return this;
		}

		public SPath CreateFile(string file)
		{
			return CreateFile(new SPath(file));
		}

		public SPath CreateFile(SPath file)
		{
			ThrowIfNotInitialized();
			if (!file.IsRelative)
				throw new ArgumentException(
					"You cannot call CreateFile() on an existing path with a non relative argument");
			return Combine(file).CreateFile();
		}

		public SPath CreateDirectory()
		{
			ThrowIfNotInitialized();
			ThrowIfRelative();

			if (IsRoot)
				throw new NotSupportedException(
					"CreateDirectory is not supported on a root level directory because it would be dangerous:" +
					ToString());

			FileSystem.DirectoryCreate(MakeAbsolute());
			return this;
		}

		public SPath CreateDirectory(string directory)
		{
			return CreateDirectory(new SPath(directory));
		}

		public SPath CreateDirectory(SPath directory)
		{
			ThrowIfNotInitialized();
			if (!directory.IsRelative)
				throw new ArgumentException("Cannot call CreateDirectory with an absolute argument");

			return Combine(directory).CreateDirectory();
		}

		public SPath Copy(string dest)
		{
			return Copy(new SPath(dest));
		}

		public SPath Copy(string dest, Func<SPath, bool> fileFilter)
		{
			return Copy(new SPath(dest), fileFilter);
		}

		public SPath Copy(SPath dest)
		{
			return Copy(dest, p => true);
		}

		public SPath Copy(SPath dest, Func<SPath, bool> fileFilter)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(dest);

			if (dest.IsRelative)
				dest = Parent.Combine(dest);

			if (dest.DirectoryExists())
				return CopyWithDeterminedDestination(dest.Combine(FileName), fileFilter);

			return CopyWithDeterminedDestination(dest, fileFilter);
		}

		public SPath MakeAbsolute()
		{
			ThrowIfNotInitialized();

			if (!IsRelative)
				return this;

			return SPath.CurrentDirectory.Combine(this);
		}

		SPath CopyWithDeterminedDestination(SPath absoluteDestination, Func<SPath, bool> fileFilter)
		{
			if (absoluteDestination.IsRelative)
				throw new ArgumentException("absoluteDestination must be absolute");

			if (FileExists())
			{
				if (!fileFilter(absoluteDestination))
					return Default;

				absoluteDestination.EnsureParentDirectoryExists();

				FileSystem.FileCopy(MakeAbsolute(), absoluteDestination.MakeAbsolute(), true);
				return absoluteDestination;
			}

			if (DirectoryExists())
			{
				absoluteDestination.EnsureDirectoryExists();
				foreach (var thing in Contents())
					thing.CopyWithDeterminedDestination(absoluteDestination.Combine(thing.RelativeTo(this)),
						fileFilter);
				return absoluteDestination;
			}

			throw new ArgumentException("Copy() called on path that doesnt exist: " + ToString());
		}

		public void Delete(DeleteMode deleteMode = DeleteMode.Normal)
		{
			ThrowIfNotInitialized();
			ThrowIfRelative();

			if (IsRoot)
				throw new NotSupportedException(
					"Delete is not supported on a root level directory because it would be dangerous:" + ToString());

			var isFile = FileExists();
			var isDir = DirectoryExists();
			if (!isFile && !isDir)
				throw new InvalidOperationException("Trying to delete a path that does not exist: " + ToString());

			try
			{
				if (isFile)
				{
					FileSystem.FileDelete(MakeAbsolute());
				}
				else
				{
					FileSystem.DirectoryDelete(MakeAbsolute(), true);
				}
			}
			catch (IOException)
			{
				if (deleteMode == DeleteMode.Normal)
					throw;
			}
		}

		public void DeleteIfExists(DeleteMode deleteMode = DeleteMode.Normal)
		{
			ThrowIfNotInitialized();
			ThrowIfRelative();

			if (FileExists() || DirectoryExists())
				Delete(deleteMode);
		}

		public SPath DeleteContents()
		{
			ThrowIfNotInitialized();
			ThrowIfRelative();

			if (IsRoot)
				throw new NotSupportedException(
					"DeleteContents is not supported on a root level directory because it would be dangerous:" +
					ToString());

			if (FileExists())
				throw new InvalidOperationException("It is not valid to perform this operation on a file");

			if (DirectoryExists())
			{
				try
				{
					Files().Delete();
					Directories().Delete();
				}
				catch (IOException)
				{
					if (Files(true).Any())
						throw;
				}

				return this;
			}

			return EnsureDirectoryExists();
		}

		public static SPath CreateTempDirectory(string myprefix)
		{
			var random = new Random();
			while (true)
			{
				var candidate = new SPath(FileSystem.TempPath+ "/" + myprefix + "_" + random.Next());
				if (!candidate.Exists())
					return candidate.CreateDirectory();
			}
		}

		public static SPath GetTempFilename(string myprefix = "")
		{
			var random = new Random();
			var prefix = FileSystem.TempPath+ "/" + (String.IsNullOrEmpty(myprefix) ? "" : myprefix + "_");
			while (true)
			{
				var candidate = new SPath(prefix + random.Next());
				if (!candidate.Exists())
					return candidate;
			}
		}

		public SPath Move(string dest)
		{
			return Move(new SPath(dest));
		}

		public SPath Move(SPath dest)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(dest);

			if (IsRoot)
				throw new NotSupportedException(
					"Move is not supported on a root level directory because it would be dangerous:" + ToString());

			if (IsRelative)
				return MakeAbsolute().Move(dest);

			if (dest.IsRelative)
				return Move(Parent.Combine(dest));

			if (dest.DirectoryExists())
				return Move(dest.Combine(FileName));

			if (FileExists())
			{
				dest.DeleteIfExists();
				dest.EnsureParentDirectoryExists();
				FileSystem.FileMove(MakeAbsolute(), dest.MakeAbsolute());
				return dest;
			}

			if (DirectoryExists())
			{
				FileSystem.DirectoryMove(MakeAbsolute(), dest.MakeAbsolute());
				return dest;
			}

			throw new ArgumentException(
				"Move() called on a path that doesn't exist: " + MakeAbsolute().ToString());
		}

		public SPath WriteAllText(string contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllText(MakeAbsolute(), contents);
			return this;
		}

		public string ReadAllText()
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllText(MakeAbsolute());
		}

		public SPath WriteAllText(string contents, Encoding encoding)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllText(MakeAbsolute(), contents, encoding);
			return this;
		}

		public string ReadAllText(Encoding encoding)
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllText(MakeAbsolute(), encoding);
		}

		public SPath WriteLines(string[] contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteLines(MakeAbsolute(), contents);
			return this;
		}

		public SPath WriteAllLines(string[] contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllLines(MakeAbsolute(), contents);
			return this;
		}

		public string[] ReadAllLines()
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllLines(MakeAbsolute());
		}

		public SPath WriteAllBytes(byte[] contents)
		{
			ThrowIfNotInitialized();
			EnsureParentDirectoryExists();
			FileSystem.WriteAllBytes(MakeAbsolute(), contents);
			return this;
		}

		public byte[] ReadAllBytes()
		{
			ThrowIfNotInitialized();
			return FileSystem.ReadAllBytes(MakeAbsolute());
		}

		public Stream OpenRead()
		{
			ThrowIfNotInitialized();
			return FileSystem.OpenRead(MakeAbsolute());
		}

		public Stream OpenWrite(FileMode mode)
		{
			ThrowIfNotInitialized();
			return FileSystem.OpenWrite(MakeAbsolute(), mode);
		}


		public IEnumerable<SPath> CopyFiles(SPath destination, bool recurse, Func<SPath, bool> fileFilter = null)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(destination);

			destination.EnsureDirectoryExists();
			var _this = this;
			return Files(recurse).Where(fileFilter ?? AlwaysTrue)
				.Select(file => file.Copy(destination.Combine(file.RelativeTo(_this)))).ToArray();
		}

		public IEnumerable<SPath> MoveFiles(SPath destination, bool recurse, Func<SPath, bool> fileFilter = null)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(destination);

			if (IsRoot)
				throw new NotSupportedException(
					"MoveFiles is not supported on this directory because it would be dangerous:" + ToString());

			destination.EnsureDirectoryExists();
			var _this = this;
			return Files(recurse).Where(fileFilter ?? AlwaysTrue)
				.Select(file => file.Move(destination.Combine(file.RelativeTo(_this)))).ToArray();
		}

		#endregion

		#region special paths

		private static SPath currentDirectory;
		public static SPath CurrentDirectory {
			get {
				if (!currentDirectory.IsInitialized)
					currentDirectory = new SPath(FileSystem.CurrentDirectory);
				return currentDirectory;
			}
		}

		private static SPath homeDirectory;
		public static SPath HomeDirectory {
			get {
				if (!homeDirectory.IsInitialized)
					homeDirectory = new SPath(FileSystem.HomeDirectory);
				return homeDirectory;
			}
		}

		private static SPath localAppData;
		public static SPath LocalAppData {
			get {
				if (!localAppData.IsInitialized)
					localAppData = new SPath(FileSystem.LocalAppData);
				return localAppData;
			}
		}

		private static SPath commonAppData;
		public static SPath CommonAppData {
			get {
				if (!commonAppData.IsInitialized)
					commonAppData = new SPath(FileSystem.CommonAppData);
				return commonAppData;
			}
		}

		private static SPath systemTemp;
		public static SPath SystemTemp {
			get {
				if (!systemTemp.IsInitialized)
					systemTemp = new SPath(FileSystem.TempPath);
				return systemTemp;
			}
		}

		#endregion

		private void ThrowIfRelative()
		{
			if (IsRelative)
				throw new ArgumentException(
					"You are attempting an operation on a Path that requires an absolute path, but the path is relative");
		}

		private void ThrowIfRoot()
		{
			if (IsRoot)
				throw new ArgumentException(
					"You are attempting an operation that is not valid on a root level directory");
		}

		private void ThrowIfNotInitialized()
		{
			if (!IsInitialized)
				throw new InvalidOperationException("You are attemping an operation on an null path");
		}

		private static void ThrowIfNotInitialized(SPath path)
		{
			path.ThrowIfNotInitialized();
		}

		public SPath EnsureDirectoryExists(string append = "")
		{
			ThrowIfNotInitialized();

			if (String.IsNullOrEmpty(append))
			{
				if (DirectoryExists())
					return this;
				EnsureParentDirectoryExists();
				CreateDirectory();
				return this;
			}
			return EnsureDirectoryExists(new SPath(append));
		}

		public SPath EnsureDirectoryExists(SPath append)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(append);

			var combined = Combine(append);
			if (combined.DirectoryExists())
				return combined;
			combined.EnsureParentDirectoryExists();
			combined.CreateDirectory();
			return combined;
		}

		public SPath EnsureParentDirectoryExists()
		{
			ThrowIfNotInitialized();

			var parent = Parent;
			parent.EnsureDirectoryExists();
			return parent;
		}

		public SPath FileMustExist()
		{
			ThrowIfNotInitialized();

			if (!FileExists())
				throw new FileNotFoundException("File was expected to exist : " + ToString());

			return this;
		}

		public SPath DirectoryMustExist()
		{
			ThrowIfNotInitialized();

			if (!DirectoryExists())
				throw new DirectoryNotFoundException("Expected directory to exist : " + ToString());

			return this;
		}

		public bool IsChildOf(string potentialBasePath)
		{
			return IsChildOf(new SPath(potentialBasePath));
		}

		public bool IsChildOf(SPath potentialBasePath)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(potentialBasePath);

			if ((IsRelative && !potentialBasePath.IsRelative) || !IsRelative && potentialBasePath.IsRelative)
				throw new ArgumentException("You can only call IsChildOf with two relative paths, or with two absolute paths");

			// If the other path is the root directory, then anything is a child of it as long as it's not a Windows path
			if (potentialBasePath.IsRoot)
			{
				if (_driveLetter != potentialBasePath._driveLetter)
					return false;
				return true;
			}

			if (IsEmpty)
				return false;

			if (Equals(potentialBasePath))
				return true;

			return Parent.IsChildOf(potentialBasePath);
		}

		public IEnumerable<SPath> RecursiveParents {
			get {
				ThrowIfNotInitialized();
				var candidate = this;
				while (true)
				{
					if (candidate.IsEmpty)
						yield break;

					candidate = candidate.Parent;
					yield return candidate;
				}
			}
		}

		public SPath ParentContaining(string needle)
		{
			return ParentContaining(new SPath(needle));
		}

		public SPath ParentContaining(SPath needle)
		{
			ThrowIfNotInitialized();
			ThrowIfNotInitialized(needle);
			ThrowIfRelative();

			return RecursiveParents.FirstOrDefault(p => p.Exists(needle));
		}

		static bool AlwaysTrue(SPath p)
		{
			return true;
		}

		private static IFileSystem _fileSystem;
		public static IFileSystem FileSystem {
			get {
				if (_fileSystem == null)
#if UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
#if UNITY_EDITOR
					_fileSystem = new FileSystem(Directory.GetCurrentDirectory());
#else
					_fileSystem = new FileSystem(UnityEngine.Application.dataPath);
#endif
#else
					_fileSystem = new FileSystem(Directory.GetCurrentDirectory());
#endif
				return _fileSystem;
			}
			set { _fileSystem = value; }
		}

		public static bool IsUnix => FileSystem.IsLinux || FileSystem.IsMac;
		public static bool IsWindows => FileSystem.IsWindows;
		public static bool IsLinux => FileSystem.IsLinux;
		public static bool IsMac => FileSystem.IsMac;

		private static StringComparison? _pathStringComparison;
		private static StringComparison PathStringComparison {
			get {
				// this is lazily evaluated because IsUnix uses the FileSystem object and that can be set
				// after static constructors happen here
				if (!_pathStringComparison.HasValue)
					_pathStringComparison = IsUnix ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
				return _pathStringComparison.Value;
			}
		}

		internal string DebuggerDisplay => ToString();
	}

#if NICEIO_INTERNAL
	internal
#else
	public
#endif
		static class Extensions
	{
		public static IEnumerable<SPath> Copy(this IEnumerable<SPath> self, string dest)
		{
			return Copy(self, new SPath(dest));
		}

		public static IEnumerable<SPath> Copy(this IEnumerable<SPath> self, SPath dest)
		{
			if (dest.IsRelative)
				throw new ArgumentException("When copying multiple files, the destination cannot be a relative path");
			dest.EnsureDirectoryExists();
			return self.Select(p => p.Copy(dest.Combine(p.FileName))).ToArray();
		}

		public static IEnumerable<SPath> Move(this IEnumerable<SPath> self, string dest)
		{
			return Move(self, new SPath(dest));
		}

		public static IEnumerable<SPath> Move(this IEnumerable<SPath> self, SPath dest)
		{
			if (dest.IsRelative)
				throw new ArgumentException("When moving multiple files, the destination cannot be a relative path");
			dest.EnsureDirectoryExists();
			return self.Select(p => p.Move(dest.Combine(p.FileName))).ToArray();
		}

		public static IEnumerable<SPath> Delete(this IEnumerable<SPath> self)
		{
			foreach (var p in self)
				p.Delete();
			return self;
		}

		public static IEnumerable<string> InQuotes(this IEnumerable<SPath> self, SlashMode forward = SlashMode.Native)
		{
			return self.Select(p => p.InQuotes(forward));
		}

		public static SPath ToSPath(this string path)
		{
			if (path == null)
				return SPath.Default;
			return new SPath(path);
		}

		public static SPath Resolve(this SPath path)
		{
			if (!path.IsInitialized || !path.Exists())
				return path;

			// because Unity sometimes lies about where things are
			string fullPath = SPath.FileSystem.GetFullPath(path.ToString());
			if (!SPath.IsUnix)
				return fullPath.ToSPath();
			return SPath.FileSystem.Resolve(fullPath).ToSPath();
		}

		public static SPath CreateTempDirectory(this SPath baseDir, string myprefix = "")
		{
			var random = new Random();
			while (true)
			{
				var candidate = baseDir.Combine(myprefix + "_" + random.Next());
				if (!candidate.Exists())
					return candidate.CreateDirectory();
			}
		}
	}

#if NICEIO_INTERNAL
	internal
#else
	public
#endif
		enum SlashMode
	{
		Native,
		Forward,
		Backward
	}

#if NICEIO_INTERNAL
	internal
#else
	public
#endif
		enum DeleteMode
	{
		Normal,
		Soft
	}

#if NICEIO_INTERNAL
	internal
#else
	public
#endif
		interface IFileSystem
	{
		string ChangeExtension(string path, string extension);
		string Combine(string path1, string path2);
		string Combine(string path1, string path2, string path3);
		void DirectoryCreate(string path);
		void DirectoryDelete(string path, bool recursive);
		bool DirectoryExists(string path);
		void DirectoryMove(string toString, string s);
		bool ExistingPathIsDirectory(string path);
		void FileCopy(string sourceFileName, string destFileName, bool overwrite);
		void FileDelete(string path);
		bool FileExists(string path);
		void FileMove(string sourceFileName, string s);

		IEnumerable<string> GetDirectories(string path);
		IEnumerable<string> GetDirectories(string path, string pattern);
		IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption);
		string GetFileNameWithoutExtension(string fileName);
		IEnumerable<string> GetFiles(string path);
		IEnumerable<string> GetFiles(string path, string pattern);
		IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption);
		string GetFullPath(string path);
		string GetRandomFileName();
		string GetFolderPath(Environment.SpecialFolder folder);
		Stream OpenRead(string path);
		Stream OpenWrite(string path, FileMode mode);
		byte[] ReadAllBytes(string path);
		string[] ReadAllLines(string path);
		string ReadAllText(string path);
		string ReadAllText(string path, Encoding encoding);
		void WriteAllBytes(string path, byte[] bytes);
		void WriteAllLines(string path, string[] contents);
		void WriteAllText(string path, string contents);
		void WriteAllText(string path, string contents, Encoding encoding);
		void WriteLines(string path, string[] contents);

		string Resolve(string path);

		char DirectorySeparatorChar { get; }

		string TempPath { get; }
		string CurrentDirectory { get; set; }
		string HomeDirectory { get; set; }
		string LocalAppData { get; set; }
		string CommonAppData { get; set; }
		bool IsWindows { get; set; }
		bool IsLinux { get; set; }
		bool IsMac { get; set; }
	}


#if NICEIO_INTERNAL
	internal
#else
	public
#endif
		class FileSystem : IFileSystem
	{
		private static Func<string, string> getCompleteRealPathFunc = null;
		private string commonAppData;
		private string currentDirectory;

		private string homeDirectory;
		private bool? isLinux;
		private bool? isMac;
		private bool? isWindows;
		private string localAppData;
		private string processDirectory;

		public FileSystem()
		{}

		/// <summary>
		/// Initialize the filesystem object with the path passed in set as the current directory
		/// </summary>
		/// <param name="directory">Current directory</param>
		public FileSystem(string directory)
		{
			currentDirectory = directory;
		}

		public string GetFolderPath(Environment.SpecialFolder folder)
		{
			switch (folder)
			{
				case Environment.SpecialFolder.LocalApplicationData:
					if (localAppData == null)
					{
						if (SPath.IsMac)
							localAppData = SPath.HomeDirectory.Combine("Library", "Application Support");
						else
							localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToSPath();
					}
					return localAppData;
				case Environment.SpecialFolder.CommonApplicationData:
					if (commonAppData == null)
					{
						if (SPath.IsWindows)
							commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).ToSPath();
						else
						{
							// there is no such thing on the mac that is guaranteed to be user accessible (/usr/local might not be)
							commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToSPath();
						}
					}
					return commonAppData;
				default:
					return "";
			}
		}

		public bool FileExists(string filename)
		{
			if (!Path.IsPathRooted(filename))
				throw new ArgumentException("FileExists requires a rooted path", "filename");
			return File.Exists(filename);
		}

		public IEnumerable<string> GetDirectories(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("GetDirectories requires a rooted path", "path");
			return Directory.GetDirectories(path);
		}

		public string Combine(string path1, string path2)
		{
			return Path.Combine(path1, path2);
		}

		public string Combine(string path1, string path2, string path3)
		{
			return Path.Combine(Path.Combine(path1, path2), path3);
		}

		public string GetFullPath(string path)
		{
			return Path.GetFullPath(path);
		}

		public bool DirectoryExists(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("DirectoryExists requires a rooted path", "path");
			return Directory.Exists(path);
		}

		public bool ExistingPathIsDirectory(string path)
		{
			var attr = File.GetAttributes(path);
			return (attr & FileAttributes.Directory) == FileAttributes.Directory;
		}

		public IEnumerable<string> GetDirectories(string path, string pattern)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("GetDirectories requires a rooted path", "path");
			return Directory.GetDirectories(path, pattern);
		}

		public IEnumerable<string> GetDirectories(string path, string pattern, SearchOption searchOption)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("GetDirectories requires a rooted path", "path");
			return Directory.GetDirectories(path, pattern, searchOption);
		}

		public string ChangeExtension(string path, string extension)
		{
			return Path.ChangeExtension(path, extension);
		}

		public string GetFileNameWithoutExtension(string fileName)
		{
			return Path.GetFileNameWithoutExtension(fileName);
		}

		public IEnumerable<string> GetFiles(string path)
		{
			return GetFiles(path, "*");
		}

		public IEnumerable<string> GetFiles(string path, string pattern)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("GetFiles requires a rooted path", "path");
			return Directory.GetFiles(path, pattern);
		}

		public string Resolve(string path)
		{
			return GetCompleteRealPath(path);
		}

		public IEnumerable<string> GetFiles(string path, string pattern, SearchOption searchOption)
		{
			foreach (var file in GetFiles(path, pattern))
				yield return file;

			if (searchOption != SearchOption.AllDirectories)
				yield break;


			if (SPath.IsUnix)
			{
				try
				{
					path = Resolve(path);
				}
				catch
				{}
			}

			foreach (var dir in GetDirectories(path))
			{
				var realdir = dir;

				if (SPath.IsUnix)
				{
					try
					{
						realdir = Resolve(dir);
					}
					catch
					{}
				}

				if (path != realdir)
				{
					foreach (var file in GetFiles(dir, pattern, searchOption))
						yield return file;
				}
			}
		}

		public byte[] ReadAllBytes(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("ReadAllBytes requires a rooted path", "path");
			return File.ReadAllBytes(path);
		}

		public void WriteAllBytes(string path, byte[] bytes)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("WriteAllBytes requires a rooted path", "path");
			File.WriteAllBytes(path, bytes);
		}

		public void DirectoryCreate(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("DirectoryCreate requires a rooted path", "path");
			Directory.CreateDirectory(path);
		}

		public void FileCopy(string sourceFileName, string destFileName, bool overwrite)
		{
			if (!Path.IsPathRooted(sourceFileName))
				throw new ArgumentException("FileCopy requires a rooted path", "sourceFileName");
			if (!Path.IsPathRooted(destFileName))
				throw new ArgumentException("FileCopy requires a rooted path", "destFileName");
			File.Copy(sourceFileName, destFileName, overwrite);
		}

		public void FileDelete(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("FileDelete requires a rooted path", "path");
			File.Delete(path);
		}

		public void DirectoryDelete(string path, bool recursive)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("DirectoryDelete requires a rooted path", "path");
			Directory.Delete(path, recursive);
		}

		public void FileMove(string sourceFileName, string destFileName)
		{
			if (!Path.IsPathRooted(sourceFileName))
				throw new ArgumentException("FileMove requires a rooted path", "sourceFileName");
			if (!Path.IsPathRooted(destFileName))
				throw new ArgumentException("FileMove requires a rooted path", "destFileName");
			File.Move(sourceFileName, destFileName);
		}

		public void DirectoryMove(string source, string dest)
		{
			if (!Path.IsPathRooted(source))
				throw new ArgumentException("DirectoryMove requires a rooted path", "source");
			if (!Path.IsPathRooted(dest))
				throw new ArgumentException("DirectoryMove requires a rooted path", "dest");
			Directory.Move(source, dest);
		}

		public void WriteAllText(string path, string contents)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("WriteAllText requires a rooted path", "path");
			File.WriteAllText(path, contents);
		}

		public void WriteAllText(string path, string contents, Encoding encoding)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("WriteAllText requires a rooted path", "path");
			File.WriteAllText(path, contents, encoding);
		}

		public string ReadAllText(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("ReadAllText requires a rooted path", "path");
			return File.ReadAllText(path);
		}

		public string ReadAllText(string path, Encoding encoding)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("ReadAllText requires a rooted path", "path");
			return File.ReadAllText(path, encoding);
		}

		public void WriteAllLines(string path, string[] contents)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("WriteAllLines requires a rooted path", "path");
			File.WriteAllLines(path, contents);
		}

		public string[] ReadAllLines(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("ReadAllLines requires a rooted path", "path");
			return File.ReadAllLines(path);
		}

		public void WriteLines(string path, string[] contents)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("WriteLines requires a rooted path", "path");
			using (var fs = File.AppendText(path))
			{
				foreach (var line in contents)
					fs.WriteLine(line);
			}
		}

		public string GetRandomFileName()
		{
			return Path.GetRandomFileName();
		}

		public Stream OpenRead(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("OpenRead requires a rooted path", "path");
			return File.OpenRead(path);
		}

		public Stream OpenWrite(string path, FileMode mode)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("OpenWrite requires a rooted path", "path");
			return new FileStream(path, mode);
		}

		public string CurrentDirectory
		{
			get => currentDirectory ?? Directory.GetCurrentDirectory();
			set
			{
				if (!Path.IsPathRooted(value))
					throw new ArgumentException("SetCurrentDirectory requires a rooted path", "directory");
				currentDirectory = value;
			}
		}

		public bool IsWindows
		{
			get
			{
				if (isWindows.HasValue)
					return isWindows.Value;
				return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
			}
			set => isWindows = value;
		}

		public bool IsLinux
		{
			get
			{
				if (isLinux.HasValue)
					return isLinux.Value;
				return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc");
			}
			set => isLinux = value;
		}

		public bool IsMac
		{
			get
			{
				if (isMac.HasValue)
					return isMac.Value;
				// most likely it'll return the proper id but just to be on the safe side, have a fallback
				return Environment.OSVersion.Platform == PlatformID.MacOSX ||
						(Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
			}
			set => isMac = value;
		}

		public string HomeDirectory
		{
			get
			{
				if (homeDirectory == null)
				{
					if (SPath.IsUnix)
						homeDirectory = new SPath(Environment.GetEnvironmentVariable("HOME"));
					else
						homeDirectory = new SPath(Environment.GetEnvironmentVariable("USERPROFILE"));
				}
				return homeDirectory;
			}
			set => homeDirectory = value;
		}

		public string LocalAppData
		{
			get => localAppData ?? (localAppData = GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
			set => localAppData = value;
		}

		public string CommonAppData
		{
			get => commonAppData ?? (localAppData = GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			set => commonAppData = value;
		}

		public string TempPath => Path.GetTempPath();

		private static Func<string, string> GetCompleteRealPath
		{
			get
			{
				if (getCompleteRealPathFunc == null)
				{
					var asm = AppDomain.CurrentDomain.GetAssemblies()
									.FirstOrDefault(x => x.FullName.StartsWith("Mono.Posix"));
					if (asm != null)
					{
						var type = asm.GetType("Mono.Unity.UnixPath");
						if (type != null)
						{
							var method = type.GetMethod("GetCompleteRealPath",
								BindingFlags.Static | BindingFlags.Public);
							if (method != null)
							{
								getCompleteRealPathFunc = (p) => {
									var ret = method.Invoke(null, new object[] { p.ToString() });
									if (ret != null)
										return ret.ToString();
									return p;
								};
							}
						}
					}

					if (getCompleteRealPathFunc == null)
						getCompleteRealPathFunc = p => p;
				}
				return getCompleteRealPathFunc;
			}
		}

		public char DirectorySeparatorChar {
			get { return Path.DirectorySeparatorChar; }
		}
	}
}
