using System;
using System.Diagnostics;

namespace AutoDI.Build
{
	public static class StackTracer
	{
		public static AdditionalInformation GetStackTrace(Exception exception)
		{
			var trace = new StackTrace(exception, true);
			var reflectedType = trace.GetFrame(0).GetMethod().ReflectedType;
			var additionalInformation = new AdditionalInformation();
			if (reflectedType != null)
			{
				additionalInformation = new AdditionalInformation()
				{
					Column = trace.GetFrame(0).GetFileColumnNumber(),
					Line = trace.GetFrame(0).GetFileLineNumber(),
					MethodName = reflectedType.FullName,
					File = trace.GetFrame(0).GetFileName()
				};

			}
			return additionalInformation;
		}
	}

	public class AdditionalInformation
	{
		public string MethodName { get; set; }
		public string File { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
	}
}
