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

Asp.net dynamic host path:
AspHandler
AspHost.ProcessPageFromFile
AspPageDom.processPage - After this method completes, the literals and code statements are separated. Literals are in a list and replaced with response.write(literal[x]). _sb has all of the code statements as unparsed text.
AspHost.CompilePage - Creates a Microsoft.Scripting.Hosting.ScriptSource using a VBScriptStringContentProvider (provided with the AspPageDom)
VBScriptContext.CompileSourceCode - called via Microsoft.Scripting (registered with the script engine).
VBScript.ParseFileToLambda (gets a VBScriptSourceCodeReader, not a plain TextReader)
	VB.Parser().ParseScriptFile(VB.Scanner) to create a ScriptBlock
	VBScriptAnalyzer.AnalyzeFile - Put module variables and functions into the scope
	loops and uses VBScriptGenerator to create dynamic code from the script
	
I want to have an analysis phase that can identitify things like include pages, variable types, etc.
Does that mean loading and parsing the whole site to be able to parse?
I think I'd like discrete parsing steps to make it documenting. But I'm also okay with quick and dirty and not architected well.
How much is general logic, and how much is MS2 transpiler?
One analysis/transformation pass is to remove the MS2 COM object. Or does that just happen at output time?
Defining variables would be nice too. Can I correct the case of variables to match their first usage?
Can I output to C# instead of VB?
Can I identitify variables that are being written to a URL and url encode them?
Can I identitify variables that are being concatenated into a SQL string?
Can I migrate from ADO, maybe with a wrapper interface or helper class?
need to preserve comments, currently they look striped.
strip extra blank lines?

Real process:
-Parse everything to find all include files. use a full canonical path to identify them.
-Transpile all the includes. I need to know they are includes because I'd probably render them differently. Possibly as classes? Or maybe a partial view? That needs discovery.
-Then transpile all the files, referencing the include files some how
-Replace MS2.Session with Session.
-Some include files we'll want to NOP, some will map just to code functions (static usings?)
ADO mapping:

main use is via a DataConn variable, which is an ADODB.Connection object
also creates ADODB.Command and ADODB.Recordset objects. We could text replace those with adapter objects.

next steps:
find out how many include files we have
categorize the include files
start making MS2 specific overloads.
make a proxy for ADODB
convert a more complex page, find the other bits that are missing.
copy a bunch of *.asp pages to my machine (or is that bad)?
what is a good proof of concept? tdetail.asp is the hardest/best. Maybe find a page that is more complex, but also stand alone?

for an include file:
make a class that has methods, fields and a render content method.
Subs that render content need to be converted to return content and callers updated to render it.
	How do I do that?

