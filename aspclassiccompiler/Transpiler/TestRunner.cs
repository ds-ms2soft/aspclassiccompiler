using System;
using System.IO;
using NUnit.Framework;

namespace Transpiler
{
	[NUnit.Framework.TestFixture]
	public class TestRunner
	{
		
		public void TranspileOne(string fileName)
		{
			var service = new Ms2Transpiler();

			service.TranspileSingle(Path.GetFileName(fileName));
		}

		
	}
}
