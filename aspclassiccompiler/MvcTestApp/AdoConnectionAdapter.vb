Imports System.Data.OleDb

Public Class AdoConnectionAdapter
	Private Connection As OleDbConnection

	public Sub Open(connectionString As string)
		connection = new OleDbConnection(connectionString)
		Connection.Open()
	End Sub

	public Function Execute(sql As string) As Object
		Execute = New OleDbCommand(sql, Connection).ExecuteReader
	End Function

	public class ReaderAdapter
		Private _underlying As OleDbDataReader

		Sub New (underlying As OleDbDataReader)
			Me._underlying = underlying
		End Sub

	End Class
End Class
