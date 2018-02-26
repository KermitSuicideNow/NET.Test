Imports RestSharp

Public Class RestHelper(Of TRequest As Class, TResponse As {Class, New})
    Public Shared Function Execute(metodo As String, request As TRequest) As TResponse
        Dim client As New RestClient("http://localhost/OpenInvoicePeru/api")

        Dim restRequest As New RestRequest(metodo, Method.POST) With { _
                .RequestFormat = DataFormat.Json _
                }

        restRequest.AddBody(request)

        Dim restResponse = client.Execute(Of TResponse)(restRequest)
        Return restResponse.Data
    End Function

End Class