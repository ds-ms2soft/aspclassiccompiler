Namespace Includes.Database
	Public Class adovbs
		Inherits Includes.IncludesBase
		
		public Sub New(page As System.Web.Mvc.WebViewPage)
			MyBase.New(page)
		End Sub

		Public Const adOpenForwardOnly = 0
		Public Const adOpenKeyset = 1
		Public Const adOpenDynamic = 2
		Public Const adOpenStatic = 3
		Public Const adHoldRecords = 256
		Public Const adMovePrevious = 512
		Public Const adAddNew = 16778240
		Public Const adDelete = 16779264
		Public Const adSimpleRecord = 0
		Public Const adCollectionRecord = 1
		Public Const adStructDoc = 2
	End Class
End Namespace
