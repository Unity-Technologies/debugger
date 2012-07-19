using System;
using CodeEditor.Debugger.Backend;

namespace CodeEditor.Debugger
{
	public interface ITypeMirrorProvider
	{
		event Action<ITypeMirror> TypeLoaded;
		event Action<ITypeMirror> TypeUnloaded;
		ITypeMirror[] LoadedTypesMirror { get; }
	}
}