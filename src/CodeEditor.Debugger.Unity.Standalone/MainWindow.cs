using System;
using System.IO;
using System.Linq;
using CodeEditor.Composition;
using CodeEditor.Debugger.Unity.Engine;
using Mono.Debugger.Soft;
using UnityEngine;
using Event = Mono.Debugger.Soft.Event;
using EventType = UnityEngine.EventType;

namespace CodeEditor.Debugger.Unity.Standalone
{
	[Export]
	class MainWindow
	{
		private readonly SourceWindow _sourceWindow;
		private readonly ConsoleWindow _console;
		
		private CallStackDisplay _callStackDisplay;
		private readonly DebuggerSession _debuggingSession;
		private ThreadsDisplay _threadDisplay;
		private DebuggerWindowManager _windowManager;

		[ImportingConstructor]
		public MainWindow(SourceWindow sourceWindow, ConsoleWindow console)
		{
			_sourceWindow = sourceWindow;
			_console = console;
	
			Camera.main.backgroundColor = new Color(0.125f,0.125f,0.125f,0);

			_debuggingSession = new DebuggerSession();
			_debuggingSession.TraceCallback += s => Trace(s);
			_debuggingSession.Start(DebuggerPortFromCommandLine());
			_debuggingSession.VMGotSuspended += OnVMGotSuspended;

			SetupDebuggingWindows();

			AdjustLayout();
		}

		private void SetupDebuggingWindows()
		{
			_windowManager = new DebuggerWindowManager();
			_windowManager.Add("Log",_console.OnGUI);

			_callStackDisplay = new CallStackDisplay(frame => ShowSourceLocation(frame.Location));
			_windowManager.Add("CallStack", _callStackDisplay.OnGUI);

			_threadDisplay = new ThreadsDisplay(_debuggingSession);
			//_windowManager.Add("Threads", _threadDisplay.OnGUI);
		}


		private void OnVMGotSuspended(Event e)
		{
			var stackFrames = _debuggingSession.GetMainThread().GetFrames();
			_callStackDisplay.SetCallFrames(stackFrames);

			_threadDisplay.SetThreads(_debuggingSession.GetThreads());

			if (stackFrames.Any())
			{
				var topFrame = stackFrames[0];
				ShowSourceLocation(topFrame.Location);
			}
			
		}

		public void OnGUI()
		{
			if (UnityEngine.Event.current.type == EventType.Layout)
				_debuggingSession.Update();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(_debuggingSession.IsConnected ? "Detach" : "Attach"))
				ToggleConnectionState();

			DoExecutionFlowUI();
			GUILayout.EndHorizontal();

			_windowManager.OnGUI();
			_sourceWindow.OnGUI();
		}



		private void DoExecutionFlowUI()
		{
			GUI.enabled = _debuggingSession.Suspended;// && !_debuggingSession.WaitingForResponse;
			if (GUILayout.Button("Continue"))
				_debuggingSession.SafeResume();
			if (GUILayout.Button("Step Over"))
				_debuggingSession.SendStepRequest(StepDepth.Over);
			if (GUILayout.Button("Step In"))
				_debuggingSession.SendStepRequest(StepDepth.Into);

			GUI.enabled = !_debuggingSession.Suspended;// && !_debuggingSession.WaitingForResponse;
			if (GUILayout.Button("Break"))
				_debuggingSession.Break();

			GUI.enabled = true;
		}

		private void AdjustLayout()
		{
			var srcViewPort = new Rect(0, ToolbarHeight, Screen.width, Screen.height * .6f - ToolbarHeight);
			_sourceWindow.ViewPort = srcViewPort;

			var consoleTop = srcViewPort.yMax + VerticalSpacing;
			_windowManager.ViewPort = new Rect(0, consoleTop, Screen.width, Screen.height - consoleTop);
		}

		const int ToolbarHeight = 20;
		const int VerticalSpacing = 4;

		private void ToggleConnectionState()
		{
			if (_debuggingSession.IsConnected)
				_debuggingSession.Disconnect();
			else
				Connect();
		}

		void Connect()
		{
			_debuggingSession.Start(DebuggerPortFromCommandLine());
		}

		private int DebuggerPortFromCommandLine()
		{
			var args = Environment.GetCommandLineArgs();
			return int.Parse(args[args.Length - 1]);
		}

		

		private void ShowSourceLocation(Location location)
		{
			if (!IsValidLocation(location))
				return;
			Trace("{0}:{1}", location.SourceFile, location.LineNumber);
			_sourceWindow.ShowSourceLocation(location.SourceFile, location.LineNumber);
		}

		private static bool IsValidLocation(Location location)
		{
			return location.LineNumber >= 1 && File.Exists(location.SourceFile);
		}

		private void Trace(string format, params object[] args)
		{
			var text = string.Format(format, args);
			Console.WriteLine(text);
			_console.WriteLine(text);
		}

		public void OnApplicationQuit()
		{
			_debuggingSession.Disconnect();
		}
	}
}
