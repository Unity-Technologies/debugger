﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEditor.Composition;
using Mono.Debugger.Soft;

namespace CodeEditor.Debugger.Implementation
{
	[Export(typeof(ISourceToTypeMapper))]
	class SourceToTypeMapper : ISourceToTypeMapper
	{
		private readonly IDebugTypeProvider _debugTypeProvider;

		private readonly Dictionary<string, List<IDebugType>> _sourceToTypes = new Dictionary<string, List<IDebugType>>();

		[ImportingConstructor]
		public SourceToTypeMapper(IDebugTypeProvider debugTypeProvider)
		{
			_debugTypeProvider = debugTypeProvider;
			_debugTypeProvider.TypeLoaded += TypeLoaded;
		}

		private void TypeLoaded(IDebugType type)
		{
			var sourceFiles = type.SourceFiles;
			foreach (var file in sourceFiles)
				AddTypeToSourceToTypes(type, file);
		}

		private void AddTypeToSourceToTypes(IDebugType type, string file)
		{
			if (!_sourceToTypes.ContainsKey(file))
				_sourceToTypes.Add(file, new List<IDebugType>());

			_sourceToTypes[file].Add(type);
		}

		public IEnumerable<IDebugType> TypesFor(string file)
		{
			if (!_sourceToTypes.ContainsKey(file))
				return new IDebugType[0];

			return _sourceToTypes[file];
		}
	}
}
