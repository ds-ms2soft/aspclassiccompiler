<B>This text is coming from HeaderInfo.asp.</B>
<pre>
	This is testing
	line
	br
	  eaks and white space
</pre>
<% 
Sub OutputDynamicText()
	Response.Write "<p>Dynamic text:&nbsp;" & Now & "</p>"
End Sub
	%>