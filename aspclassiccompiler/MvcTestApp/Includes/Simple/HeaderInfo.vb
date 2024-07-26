Namespace Includes.Simple
	Public Class HeaderInfo
		Inherits Includes.IncludesBase
		
		public Sub New(page As System.Web.Mvc.WebViewPage)
			MyBase.New(page)
			Raw("<B>This text is coming from HeaderInfo.asp.</B>
<pre>
	This is testing
	line
	br
	  eaks and white space
</pre>
")
		End Sub

		Public Sub OutputDynamicText()
			Raw("<p>Dynamic text:&nbsp;" & Now & "</p>")
		End Sub
	End Class
End Namespace
