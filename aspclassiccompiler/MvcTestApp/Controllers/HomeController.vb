Public Class HomeController
	Inherits System.Web.Mvc.Controller

	Function Index() As ActionResult
		Return View("ViewPage1")
	End Function

	Function Asp(id As string) As ActionResult
		Return View(id)
	End Function
End Class
