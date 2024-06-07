namespace Transpiler
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var service = new TranspilerService(@"C:\Work\aspclassiccompiler\aspclassiccompiler\AspWebApp\components\",
				@"C:\Work\aspclassiccompiler\aspclassiccompiler\MvcTestApp\Views\Home\");

			service.Transpile("FileSystem.asp");
		}
	}
}
