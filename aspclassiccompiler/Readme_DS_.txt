2024/03/30: Dave Sweeton:

I got the Vs2013 version of the solution compiling in VS2022, but I needed to download VS2019 and install the .net 4.0 targetting pack. Then VS2022 was able to build it. I had to update one MVC assembly reference.

Solution builds with warnings, but the warnings look like they are legit (i.e. there before I got here).

Thinking:

We've got a thing that parses to tokens
	VBScriptGenerator itself doesn't seem to useful as it returns dynamic expressions. Those don't seem useful for transpiling. 
	Maybe better to create a transpiler that does what VbScriptGenerator does?
	
var scanner = new VB.Scanner(reader);
var errorTable = new List<VB.SyntaxError>();

var block = new VB.Parser().ParseScriptFile(scanner, errorTable);

Block is now the file parsed to tokens.

Maybe next step is to make a "simple" transpiler and start working with one file?