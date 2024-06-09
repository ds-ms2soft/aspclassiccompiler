Public MustInherit Class CustomViewPage 
	Inherits System.Web.Mvc.WebViewPage

	
	private _dataConn As AdoConnectionAdapter
	Public ReadOnly Property DataConn As AdoConnectionAdapter
		Get
			If (_dataConn Is Nothing)
				_dataConn = New AdoConnectionAdapter
			End If
			Return _dataConn
		End Get
	End Property

	Public Sub Raw(value As string)
		Output.Write(value)
	End Sub
End Class
