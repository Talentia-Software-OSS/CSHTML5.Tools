﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSHTML5.Tools.StubMerger
{
	/// <summary>
	/// Represents an existing namespace in CSHTML5.
	/// </summary>
	public class Namespace
	{
		/// <summary>
		/// Name of the namespace
		/// </summary>
		public string Name { get; }
		
		public string FullPath { get; }
		
		/// <summary>
		/// Files containing a class part (located in the root of its namespace).
		/// Whether the class part represents the implemented or the stub part of the class can be determined with <see cref="ClassPart.IsStub"/>
		/// </summary>
		public HashSet<ClassPart> ClassParts { get; } = new HashSet<ClassPart>();
		
		public Namespace(string namespaceRoot, string namespaceName)
		{
			Name = namespaceName;
			FullPath = Path.Combine(namespaceRoot, namespaceName);
		}

		/// <summary>
		/// Checks if a namespace folder exists
		/// </summary>
		/// <param name="namespaceRoot"></param>
		/// <param name="namespaceName"></param>
		/// <returns></returns>
		public static bool Exists(string namespaceRoot, string namespaceName)
		{
			return Directory.Exists(Path.Combine(namespaceRoot, namespaceName));
		}

		/// <summary>
		/// Finds if the namespace contains a class with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="filter">Filter to only search for <see cref="ClassPart"/> with a specific type.</param>
		/// <returns></returns>
		public bool ContainsClassWithName(string name, ClassFilter filter = ClassFilter.NONE)
		{
			switch (filter)
			{
				case ClassFilter.NONE:
					return ClassParts.Any(classPart => classPart.Name == name);
				case ClassFilter.STUB:
					return ClassParts.Any(classPart => classPart.Name == name && classPart.IsStub);
				case ClassFilter.IMPLEMENTED:
					return ClassParts.Any(classPart => classPart.Name == name && !classPart.IsStub);
				default:
					throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
			}
		}

		public HashSet<ClassPart> GetClassPartsWithName(string name, ClassFilter filter = ClassFilter.NONE)
		{
			switch (filter)
			{
				case ClassFilter.NONE:
					return ClassParts.Where(classPart => classPart.Name == name).ToHashSet();
				case ClassFilter.STUB:
					return ClassParts.Where(classPart => classPart.Name == name && classPart.IsStub).ToHashSet();
				case ClassFilter.IMPLEMENTED:
					return ClassParts.Where(classPart => classPart.Name == name && !classPart.IsStub).ToHashSet();
				default:
					throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
			}
		}
		
		/// <summary>
		/// Ensure the namespace folders exists, and create them otherwise
		/// </summary>
		/// <param name="namespacePath">Path to the namespace</param>
		private static void EnsureNamespaceFoldersExists(string namespacePath)
		{
			Directory.CreateDirectory(namespacePath);
			Directory.CreateDirectory(Path.Combine(namespacePath, "WORKINPROGRESS"));
		}
		
		/// <summary>
		/// Gets all the namespaces (and their classes) available in CSHTML5.
		/// </summary>
		/// <param name="namespacesRoot">Path to the root of the namespaces available in CSHTML5.</param>
		/// <param name="namespaceName">Name of the namespaces available in CSHTML5.</param>
		/// <returns>A <see cref="Namespace"/>, containing informations about the requested namespace in CSHTML5.</returns>
		public static Namespace GetOrCreateExistingNamespace(string namespacesRoot, string namespaceName)
		{
			Namespace existingNamespace = new Namespace(namespacesRoot, namespaceName);

			EnsureNamespaceFoldersExists(existingNamespace.FullPath);

			// Get the implemented part of all classes in the namespace
			foreach (string implementedClassPath in Directory.GetFiles(existingNamespace.FullPath))
			{
				existingNamespace.ClassParts.Add(new ClassPart(existingNamespace, implementedClassPath, false));
			}

			// Get the stub part of all classes in the namespace
			string WIPPath = Path.Combine(existingNamespace.FullPath, "WORKINPROGRESS");
			foreach (string stubClassPath in Directory.GetFiles(WIPPath))
			{
				existingNamespace.ClassParts.Add(new ClassPart(existingNamespace, stubClassPath, true));
			}

			return existingNamespace;
		}

		private static string[] _excluded = { "bin", "obj", "Properties" };
		
		/// <summary>
		/// Gets all the namespaces (and their classes) generated by the StubGenerator.
		/// </summary>
		/// <param name="namespacesRoot">Path to the root of the namespaces generated by the StubGenerator.</param>
		/// <returns>A HashSet of <see cref="Namespace"/>, containing informations about each generated namespaces.</returns>
		public static HashSet<Namespace> GetGeneratedNamespaces(string namespacesRoot)
		{
			HashSet<Namespace> namespaces = new HashSet<Namespace>();

			// For each namespace folder
			foreach (string @namespace in Directory.GetDirectories(namespacesRoot).Where(dir => !_excluded.Any(dir.EndsWith)))
			{
				Namespace generatedNamespace = new Namespace(namespacesRoot, Path.GetFileName(@namespace));

				// Get all stub classes in the namespace
				foreach (string stubClass in Directory.GetFiles(generatedNamespace.FullPath))
				{
					generatedNamespace.ClassParts.Add(new ClassPart(generatedNamespace, stubClass, true));
				}

				namespaces.Add(generatedNamespace);
			}

			return namespaces;
		}
	}
}