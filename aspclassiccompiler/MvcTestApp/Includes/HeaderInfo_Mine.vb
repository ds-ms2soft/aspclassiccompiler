Namespace Includes
	Public Class HeaderInfo_Mine
		Inherits IncludesBase
	
		Public Sub New(page As WebViewPage)
			MyBase.New(page)
			Raw("""<B>This text is coming from HeaderInfo.asp.

</B>""")
		End Sub

		public Sub OutputDynamicText()
			Raw("<p>Dynamic text:&nbsp;" & Now & "</p>")
		End Sub
	End Class
End NameSpace