Public Class HomeController
	Inherits System.Web.Mvc.Controller

	Function Index() As ActionResult
		Return View("ViewPage1")
	End Function

	Function Asp() As ActionResult
		Dim viewName = Request.RawUrl.Split("?")(0).TrimStart("/")
		'TODO: Need to fix server.mappath and such to work on the raw URL.
		Return View(viewName)
	End Function
End Class
