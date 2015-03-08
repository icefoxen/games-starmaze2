using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Starmaze
{
	public static class Log
	{
		// Screw singletons.
		static string LogFileName = "log.txt";
		public static bool LogToConsole = false;

		public static void Init()
		{
			// Clear old log if it exists.
			if (File.Exists(LogFileName)) {
				File.Delete(LogFileName);
			}
			SetLogToConsole();
			// By default we just write the log file to the same place as the exe.
			/*
			var basePath = Environment.GetEnvironmentVariable("STARMAZE_HOME");
			if (basePath == null) {
				// This gets the location of the .exe, essentially.
				basePath = AppDomain.CurrentDomain.BaseDirectory;
			}
			basePath = System.IO.Path.Combine(basePath, "..");
*/
		}

		/// <summary>
		/// This method exists to cunningly make it so we write logs to a console
		/// when in debug mode but not in release mode.
		/// </summary>
		[Conditional("DEBUG")]
		static void SetLogToConsole()
		{
			LogToConsole = true;
		}

		/// <summary>
		/// Returns system info string.
		/// </summary>
		/// <returns>System info string.</returns>
		public static string GetSystemInfo()
		{
			var os = Environment.OSVersion;
			var runtimeVersion = Environment.Version;

			return String.Format("Starmaze version: {0} OS: {1}, runtime version {2}", Util.StarmazeVersion, os, runtimeVersion);
		}
		// These Conditional() flags mark these methods as never getting called if we are not
		// making a DEBUG build.  Monodevelop sets the DEBUG #define or not based
		// on project build mode.  Supposedly; in reality, it seems a little conservative
		// about rebuilding the project when you switch modes, and has to be forced
		// to rebuild.  Stupid.
		[Conditional("DEBUG")]
		public static void Assert(bool assertion)
		{
			Log.Assert(assertion, "");
		}

		public static void Assert(bool assertion, string message, params object[] args)
		{
			if (!assertion) {
				Log.Message("ASSERTION FAILED:");
				Log.Message(message, args);
				Log.Message(new StackTrace().ToString());
				// Environment.Exit would be another way of doing this
				// But we really should pop up at least SOME form of dialog box
				// and this honestly doesn't do a terrible job.
				Debug.Assert(assertion, String.Format(message, args));
			}
		}

		[Conditional("DEBUG")]
		public static void Warn(bool assertion, string message, params object[] args)
		{

			if (!assertion) {
				Log.Message("WARNING:");
				Log.Message(message, args);
				Log.Message(new StackTrace().ToString());
			}
		}
		// Do we want a debug-only version of this?  That's _kind_ of the above Warn() method...
		public static void Message(string message, params object[] args)
		{
			var text = String.Format(message, args);
			File.AppendAllText(LogFileName, text);
			File.AppendAllText(LogFileName, Environment.NewLine);
			// We always write to the log file, but optionally write to console as well.
			if (LogToConsole) {
				Console.WriteLine(text);
			}
		}
	}
}

