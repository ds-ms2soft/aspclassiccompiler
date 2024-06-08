Public MustInherit Class IncludesBase
	Public ReadOnly Property Page As WebViewPage

	public Sub New(page as System.Web.Mvc.WebViewPage )
		Me.Page = page
		Me.Html = page.Html
	End Sub

	Public ReadOnly Property Html As HtmlHelper(Of Object)


	Protected Sub Raw(value as string)
		Page.Output.Write(value)
	End Sub
End Class
