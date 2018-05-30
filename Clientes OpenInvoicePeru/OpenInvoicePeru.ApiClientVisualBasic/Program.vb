Option Strict On

Imports System.IO
Imports OpenInvoicePeru.Comun.Dto.Intercambio
Imports OpenInvoicePeru.Comun.Dto.Modelos

Module Program

    Private Const UrlSunat As String = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService"
    Private Const UrlOtroCpe As String = "https://e-beta.sunat.gob.pe/ol-ti-itemision-otroscpe-gem-beta/billService"

    Private Const FormatoFecha As String = "yyyy-MM-dd"
    Private _ruc As String

    Sub Main()
        Console.ForegroundColor = ConsoleColor.Green
        Console.Title = "Clientes OpenInvoicePeru (VB.NET)"

        Console.WriteLine("Prueba de API REST de OpenInvoicePeru (VB.NET)")
        Console.WriteLine("Ingrese su numero de RUC (puede elegir cualquiera)")
        _ruc = Console.ReadLine()
        CrearFactura()
        CrearBoleta()
        CrearNotaCredito()
        CrearResumenDiario()
        CrearComunicacionBaja()
        CrearDocumentoRetencion()
        CrearDocumentoPercepcion()

        Console.ReadLine()
    End Sub

    Private Function CrearEmisor() As Contribuyente
        Return New Contribuyente() With {
            .NroDocumento = _ruc,
            .TipoDocumento = "6", 'CATALOGO N° 06
            .Direccion = "CARRETERA PIMENTEL",
            .Urbanizacion = "-",
            .Departamento = "LA LIBERTAD",
            .Provincia = "CHICLAYO",
            .Distrito = "CHICLAYO",
            .NombreComercial = "EMPRESA DE SOFTWARE",
            .NombreLegal = "EMPRESA DE SOFTWARE S.A.C.",
            .Ubigeo = "140101"
        }
    End Function

    Private Sub CrearFactura()
        Try
            Console.WriteLine("Ejemplo Factura")
            ' Gravada
            Dim documento As New DocumentoElectronico() With {
                .Emisor = CrearEmisor(),
                .Receptor = New Contribuyente() With {
                    .NroDocumento = "20100039207",
                    .TipoDocumento = "6",
                    .NombreLegal = "RANSA COMERCIAL S.A."
                },
                .IdDocumento = "FF11-001",
                .FechaEmision = DateTime.Today.ToString(FormatoFecha),
                .Moneda = "PEN",
                .TipoDocumento = "01", 'CATALOGO N° 1
                .TotalIgv = 18,
                .TotalVenta = 118,
                .Gravadas = 100,
                .Items = New List(Of DetalleDocumento)() From {
                    New DetalleDocumento() With {
                        .Id = 1,
                        .Cantidad = 5,
                        .PrecioReferencial = 20,
                        .PrecioUnitario = 20,
                        .TipoPrecio = "01", 'CATALOGO N° 16
                        .CodigoItem = "XXXXX",
                        .Descripcion = "Arroz Costeño",
                        .UnidadMedida = "KGM", 'CATALOGO N° 3
                        .Impuesto = 18,
                        .TipoImpuesto = "10", 'OPERACION GRAVADA -> CATALOGO N° 07
                        .TotalVenta = 100 'MONTO TOTAL DE VENTA SIN IGV
                    }
                }
            }

            Console.WriteLine("Generando XML....")

            Dim documentoResponse = RestHelper(Of DocumentoElectronico, DocumentoResponse).Execute("GenerarFactura", documento)

            If Not documentoResponse.Exito Then
                Throw New InvalidOperationException(documentoResponse.MensajeError)
            End If

            Console.WriteLine("Firmando XML...")
            ' Firmado del Documento.
            Dim firmado As New FirmadoRequest() With {
                .TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                .CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                .PasswordCertificado = String.Empty,
                .UnSoloNodoExtension = False
            }

            Dim responseFirma = RestHelper(Of FirmadoRequest, FirmadoResponse).Execute("Firmar", firmado)

            If Not responseFirma.Exito Then
                Throw New InvalidOperationException(responseFirma.MensajeError)
            End If

            'estos datos sirven para crear el código QR o el PDF417
            Console.WriteLine("Codigo Hash: {0} ", responseFirma.ResumenFirma) '28 caracteres
            Console.WriteLine("Valor de la firma: {0}", responseFirma.ValorFirma)

            File.WriteAllBytes("factura.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado))

            Console.WriteLine("Enviando a SUNAT....")

            Dim documentoRequest As New EnviarDocumentoRequest() With {
                .Ruc = documento.Emisor.NroDocumento,
                .UsuarioSol = "MODDATOS",
                .ClaveSol = "MODDATOS",
                .EndPointUrl = UrlSunat,
                .IdDocumento = documento.IdDocumento,
                .TipoDocumento = documento.TipoDocumento,
                .TramaXmlFirmado = responseFirma.TramaXmlFirmado
            }

            Dim enviarDocumentoResponse = RestHelper(Of EnviarDocumentoRequest, EnviarDocumentoResponse).Execute("EnviarDocumento", documentoRequest)

            If Not enviarDocumentoResponse.Exito Then
                Throw New InvalidOperationException(enviarDocumentoResponse.MensajeError)
            End If

            File.WriteAllBytes("facturacdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr))

            Console.WriteLine("Respuesta de SUNAT:")
            Console.WriteLine(enviarDocumentoResponse.CodigoRespuesta)
            Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            Console.ReadLine()
        End Try
    End Sub

    Private Sub CrearBoleta()
        Try
            Console.WriteLine("Ejemplo Boleta")
            ' Gravada
            Dim documento As New DocumentoElectronico() With {
                .Emisor = CrearEmisor(),
                .Receptor = New Contribuyente() With {
                    .NroDocumento = "88888888",
                    .TipoDocumento = "1",
                    .NombreLegal = "CLIENTE GENERICO"
                },
                .IdDocumento = "BB11-001",
                .FechaEmision = DateTime.Today.AddDays(-5).ToString(FormatoFecha),
                .Moneda = "PEN",
                .TipoDocumento = "03",
                .TotalIgv = 18,
                .TotalVenta = 118,
                .Gravadas = 100,
                .Items = New List(Of DetalleDocumento)() From {
                    New DetalleDocumento() With {
                        .Id = 1,
                        .Cantidad = 10,
                        .PrecioReferencial = 10,
                        .PrecioUnitario = 10,
                        .TipoPrecio = "01",
                        .CodigoItem = "2435675",
                        .Descripcion = "USB Kingston ©",
                        .UnidadMedida = "NIU",
                        .Impuesto = 18,
                        .TipoImpuesto = "10",
                        .TotalVenta = 100
                    }
                }
            }

            Console.WriteLine("Generando XML....")

            Dim documentoResponse = RestHelper(Of DocumentoElectronico, DocumentoResponse).Execute("GenerarFactura", documento)

            If Not documentoResponse.Exito Then
                Throw New InvalidOperationException(documentoResponse.MensajeError)
            End If

            Console.WriteLine("Firmando XML...")
            ' Firmado del Documento.
            Dim firmado As New FirmadoRequest() With {
                .TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                .CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                .PasswordCertificado = String.Empty,
                .UnSoloNodoExtension = False
            }

            Dim responseFirma = RestHelper(Of FirmadoRequest, FirmadoResponse).Execute("Firmar", firmado)

            If Not responseFirma.Exito Then
                Throw New InvalidOperationException(responseFirma.MensajeError)
            End If

            File.WriteAllBytes("boleta.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado))

            Console.WriteLine("Enviando a SUNAT....")

            Dim documentoRequest As New EnviarDocumentoRequest() With {
                .Ruc = documento.Emisor.NroDocumento,
                .UsuarioSol = "MODDATOS",
                .ClaveSol = "MODDATOS",
                .EndPointUrl = UrlSunat,
                .IdDocumento = documento.IdDocumento,
                .TipoDocumento = documento.TipoDocumento,
                .TramaXmlFirmado = responseFirma.TramaXmlFirmado
            }

            Dim enviarDocumentoResponse = RestHelper(Of EnviarDocumentoRequest, EnviarDocumentoResponse).Execute("EnviarDocumento", documentoRequest)

            If Not enviarDocumentoResponse.Exito Then
                Throw New InvalidOperationException(enviarDocumentoResponse.MensajeError)
            End If

            File.WriteAllBytes("boletacdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr))

            Console.WriteLine("Respuesta de SUNAT:")
            Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            Console.ReadLine()
        End Try
    End Sub

    Private Sub CrearNotaCredito()
        Try
            Console.WriteLine("Ejemplo Nota de Crédito de Factura")
            ' Gravada
            Dim documento As New DocumentoElectronico() With {
                .Emisor = CrearEmisor(),
                .Receptor = New Contribuyente() With {
                    .NroDocumento = "20257471609",
                    .TipoDocumento = "6",
                    .NombreLegal = "FRAMEWORK PERU"
                },
                .IdDocumento = "FN11-001",
                .FechaEmision = DateTime.Today.ToString(FormatoFecha),
                .Moneda = "PEN",
                .TipoDocumento = "07",
                .TotalIgv = 0.76D,
                .TotalVenta = 5,
                .Gravadas = 4.24D,
                .Items = New List(Of DetalleDocumento)() From {
                    New DetalleDocumento() With {
                        .Id = 1,
                        .Cantidad = 1,
                        .PrecioReferencial = 4.24D,
                        .PrecioUnitario = 4.24D,
                        .TipoPrecio = "01",
                        .CodigoItem = "2435675",
                        .Descripcion = "Correcion de Factura",
                        .UnidadMedida = "NIU",
                        .Impuesto = 0.76D,
                        .TipoImpuesto = "10",
                        .TotalVenta = 5
                    }
                },
                .Discrepancias = New List(Of Discrepancia)() From {
                    New Discrepancia() With {
                        .NroReferencia = "FF11-001",
                        .Tipo = "04", 'TIPO DE MOTIVO DE APLICACION DE NOTA -> CATALOGO N° 09 (NC)
                        .Descripcion = "Correccion del monto original" 'PARA ND -> CATALOGO N° 10
                    }
                },
                .Relacionados = New List(Of DocumentoRelacionado)() From {
                    New DocumentoRelacionado() With {
                        .NroDocumento = "FF11-001",
                        .TipoDocumento = "01" 'CATALOGO N° 1
                    }
                }
            }

            Console.WriteLine("Generando XML....")

            Dim documentoResponse = RestHelper(Of DocumentoElectronico, DocumentoResponse).Execute("GenerarNotaCredito", documento)

            If Not documentoResponse.Exito Then
                Throw New InvalidOperationException(documentoResponse.MensajeError)
            End If

            Console.WriteLine("Firmando XML...")
            ' Firmado del Documento.
            Dim firmado As New FirmadoRequest() With {
                .TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                .CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                .PasswordCertificado = String.Empty,
                .UnSoloNodoExtension = False
            }

            Dim responseFirma = RestHelper(Of FirmadoRequest, FirmadoResponse).Execute("Firmar", firmado)

            If Not responseFirma.Exito Then
                Throw New InvalidOperationException(responseFirma.MensajeError)
            End If

            File.WriteAllBytes("notacredito.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado))

            Console.WriteLine("Enviando a SUNAT....")

            Dim documentoRequest As New EnviarDocumentoRequest() With {
                .Ruc = documento.Emisor.NroDocumento,
                .UsuarioSol = "MODDATOS",
                .ClaveSol = "MODDATOS",
                .EndPointUrl = UrlSunat,
                .IdDocumento = documento.IdDocumento,
                .TipoDocumento = documento.TipoDocumento,
                .TramaXmlFirmado = responseFirma.TramaXmlFirmado
            }

            Dim enviarDocumentoResponse = RestHelper(Of EnviarDocumentoRequest, EnviarDocumentoResponse).Execute("EnviarDocumento", documentoRequest)

            If Not enviarDocumentoResponse.Exito Then
                Throw New InvalidOperationException(enviarDocumentoResponse.MensajeError)
            End If

            File.WriteAllBytes("notacreditocdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr))

            Console.WriteLine("Respuesta de SUNAT:")
            Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            Console.ReadLine()
        End Try
    End Sub

    Private Sub CrearResumenDiario()
        Try
            Console.WriteLine("Ejemplo de Resumen Diario")
            Dim documentoResumenDiario As New ResumenDiarioNuevo() With {
                .IdDocumento = String.Format("RC-{0:yyyyMMdd}-001", DateTime.Today),
                .FechaEmision = DateTime.Today.ToString(FormatoFecha),
                .FechaReferencia = DateTime.Today.AddDays(-1).ToString(FormatoFecha),
                .Emisor = CrearEmisor(),
                .Resumenes = New List(Of GrupoResumenNuevo)()
            }

            ' 1 - Agregar. 2 - Modificar. 3 - Eliminar
            documentoResumenDiario.Resumenes.Add(New GrupoResumenNuevo() With {
                .Id = 1,
                .TipoDocumento = "03",
                .IdDocumento = "BB14-33386",
                .NroDocumentoReceptor = "41614074",
                .TipoDocumentoReceptor = "1",
                .CodigoEstadoItem = 1,
                .Moneda = "PEN",
                .TotalVenta = 190.9D,
                .TotalIgv = 29.12D,
                .Gravadas = 161.78D
            })
            ' Para los casos de envio de boletas anuladas, se debe primero informar las boletas creadas (1) y luego en un segundo resumen se envian las anuladas. De lo contrario se presentará el error 'El documento indicado no existe no puede ser modificado/eliminado'
            ' 1 - Agregar. 2 - Modificar. 3 - Eliminar
            documentoResumenDiario.Resumenes.Add(New GrupoResumenNuevo() With {
                .Id = 2,
                .TipoDocumento = "03",
                .IdDocumento = "BB30-33384",
                .NroDocumentoReceptor = "08506678",
                .TipoDocumentoReceptor = "1",
                .CodigoEstadoItem = 1,
                .Moneda = "USD",
                .TotalVenta = 9580D,
                .TotalIgv = 1411.36D,
                .Gravadas = 8168.64D
            })


            Console.WriteLine("Generando XML....")

            Dim documentoResponse = RestHelper(Of ResumenDiarioNuevo, DocumentoResponse).Execute("GenerarResumenDiario/v2", documentoResumenDiario)

            If Not documentoResponse.Exito Then
                Throw New InvalidOperationException(documentoResponse.MensajeError)
            End If

            Console.WriteLine("Firmando XML...")
            ' Firmado del Documento.
            Dim firmado As New FirmadoRequest() With {
                .TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                .CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("Certificado.pfx")),
                .PasswordCertificado = String.Empty,
                .UnSoloNodoExtension = True
            }

            Dim responseFirma = RestHelper(Of FirmadoRequest, FirmadoResponse).Execute("Firmar", firmado)

            If Not responseFirma.Exito Then
                Throw New InvalidOperationException(responseFirma.MensajeError)
            End If

            Console.WriteLine("Guardando XML de Resumen....(Revisar carpeta del ejecutable)")

            File.WriteAllBytes("resumendiario.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado))

            Console.WriteLine("Enviando a SUNAT....")

            Dim enviarDocumentoRequest As New EnviarDocumentoRequest() With {
                .Ruc = documentoResumenDiario.Emisor.NroDocumento,
                .UsuarioSol = "MODDATOS",
                .ClaveSol = "MODDATOS",
                .EndPointUrl = UrlSunat,
                .IdDocumento = documentoResumenDiario.IdDocumento,
                .TramaXmlFirmado = responseFirma.TramaXmlFirmado
            }

            Dim enviarResumenResponse = RestHelper(Of EnviarDocumentoRequest, EnviarResumenResponse).Execute("EnviarResumen", enviarDocumentoRequest)

            If Not enviarResumenResponse.Exito Then
                Throw New InvalidOperationException(enviarResumenResponse.MensajeError)
            End If

            Console.WriteLine("Nro de Ticket: {0}", enviarResumenResponse.NroTicket)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            Console.ReadLine()
        End Try
    End Sub

    Private Sub CrearComunicacionBaja()
        Try
            Console.WriteLine("Ejemplo de Comunicación de Baja")
            Dim documentoBaja As New ComunicacionBaja() With {
                .IdDocumento = String.Format("RA-{0:yyyyMMdd}-001", DateTime.Today),
                .FechaEmision = DateTime.Today.ToString(FormatoFecha),
                .FechaReferencia = DateTime.Today.AddDays(-1).ToString(FormatoFecha),
                .Emisor = CrearEmisor(),
                .Bajas = New List(Of DocumentoBaja)()
            }

            ' En las comunicaciones de Baja ya no se pueden colocar boletas, ya que la anulacion de las mismas
            ' la realiza el resumen diario.
            documentoBaja.Bajas.Add(New DocumentoBaja() With {
                .Id = 1,
                .Correlativo = "33386",
                .TipoDocumento = "01",
                .Serie = "FA50",
                .MotivoBaja = "Anulación por otro tipo de documento"
            })
            documentoBaja.Bajas.Add(New DocumentoBaja() With {
                .Id = 2,
                .Correlativo = "86486",
                .TipoDocumento = "01",
                .Serie = "FF14",
                .MotivoBaja = "Anulación por otro datos erroneos"
            })

            Console.WriteLine("Generando XML....")

            Dim documentoResponse = RestHelper(Of ComunicacionBaja, DocumentoResponse).Execute("GenerarComunicacionBaja", documentoBaja)
            If Not documentoResponse.Exito Then
                Throw New InvalidOperationException(documentoResponse.MensajeError)
            End If

            Console.WriteLine("Firmando XML...")
            ' Firmado del Documento.
            Dim firmado As New FirmadoRequest() With {
                .TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                .CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("Certificado.pfx")),
                .PasswordCertificado = String.Empty,
                .UnSoloNodoExtension = True
            }

            Dim responseFirma = RestHelper(Of FirmadoRequest, FirmadoResponse).Execute("Firmar", firmado)

            If Not responseFirma.Exito Then
                Throw New InvalidOperationException(responseFirma.MensajeError)
            End If

            Console.WriteLine("Guardando XML de la Comunicacion de Baja....(Revisar carpeta del ejecutable)")

            File.WriteAllBytes("comunicacionbaja.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado))

            Console.WriteLine("Enviando a SUNAT....")

            Dim sendBill As New EnviarDocumentoRequest() With {
                .Ruc = documentoBaja.Emisor.NroDocumento,
                .UsuarioSol = "MODDATOS",
                .ClaveSol = "MODDATOS",
                .EndPointUrl = UrlSunat,
                .IdDocumento = documentoBaja.IdDocumento,
                .TramaXmlFirmado = responseFirma.TramaXmlFirmado
            }

            Dim enviarResumenResponse = RestHelper(Of EnviarDocumentoRequest, EnviarResumenResponse).Execute("EnviarResumen", sendBill)

            If Not enviarResumenResponse.Exito Then
                Throw New InvalidOperationException(enviarResumenResponse.MensajeError)
            End If

            Console.WriteLine("Nro de Ticket: {0}", enviarResumenResponse.NroTicket)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            Console.ReadLine()
        End Try
    End Sub

    Private Sub CrearDocumentoRetencion()
        Try
            Console.WriteLine("Ejemplo de Retención")
            Dim documento As New DocumentoRetencion() With {
                .Emisor = CrearEmisor(),
                .Receptor = New Contribuyente() With {
                    .NroDocumento = "20100039207",
                    .TipoDocumento = "6",
                    .NombreLegal = "RANSA COMERCIAL S.A.",
                    .Ubigeo = "150101",
                    .Direccion = "Av. Argentina 2833",
                    .Urbanizacion = "-",
                    .Departamento = "CALLAO",
                    .Provincia = "CALLAO",
                    .Distrito = "CALLAO"
                },
                .IdDocumento = "R001-123",
                .FechaEmision = DateTime.Today.ToString(FormatoFecha),
                .Moneda = "PEN",
                .RegimenRetencion = "01",
                .TasaRetencion = 3,
                .ImporteTotalRetenido = 300,
                .ImporteTotalPagado = 10000,
                .Observaciones = "Emision de Facturas del periodo Dic. 2016",
                .DocumentosRelacionados = New List(Of ItemRetencion)() From {
                    New ItemRetencion() With {
                        .NroDocumento = "E001-457",
                        .TipoDocumento = "01",
                        .MonedaDocumentoRelacionado = "USD",
                        .FechaEmision = DateTime.Today.AddDays(-3).ToString(FormatoFecha),
                        .ImporteTotal = 10000,
                        .FechaPago = DateTime.Today.ToString(FormatoFecha),
                        .NumeroPago = 153,
                        .ImporteSinRetencion = 9700,
                        .ImporteRetenido = 300,
                        .FechaRetencion = DateTime.Today.ToString(FormatoFecha),
                        .ImporteTotalNeto = 10000,
                        .TipoCambio = 3.41D,
                        .FechaTipoCambio = DateTime.Today.ToString(FormatoFecha)
                    }
                }
            }

            Console.WriteLine("Generando XML....")

            Dim documentoResponse = RestHelper(Of DocumentoRetencion, DocumentoResponse).Execute("GenerarRetencion", documento)

            If Not documentoResponse.Exito Then
                Throw New InvalidOperationException(documentoResponse.MensajeError)
            End If

            Console.WriteLine("Firmando XML...")
            ' Firmado del Documento.
            Dim firmado As New FirmadoRequest() With {
                .TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                .CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                .PasswordCertificado = String.Empty,
                .UnSoloNodoExtension = True
            }

            Dim responseFirma = RestHelper(Of FirmadoRequest, FirmadoResponse).Execute("Firmar", firmado)

            If Not responseFirma.Exito Then
                Throw New InvalidOperationException(responseFirma.MensajeError)
            End If

            File.WriteAllBytes("retencion.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado))

            Console.WriteLine("Enviando Retención a SUNAT....")

            Dim enviarDocumentoRequest As New EnviarDocumentoRequest() With {
                .Ruc = documento.Emisor.NroDocumento,
                .UsuarioSol = "MODDATOS",
                .ClaveSol = "MODDATOS",
                .EndPointUrl = UrlOtroCpe,
                .IdDocumento = documento.IdDocumento,
                .TipoDocumento = "20",
                .TramaXmlFirmado = responseFirma.TramaXmlFirmado
            }

            Dim enviarDocumentoResponse = RestHelper(Of EnviarDocumentoRequest, EnviarDocumentoResponse).Execute("EnviarDocumento", enviarDocumentoRequest)

            If Not enviarDocumentoResponse.Exito Then
                Throw New InvalidOperationException(enviarDocumentoResponse.MensajeError)
            End If

            Console.WriteLine("Respuesta de SUNAT:")
            Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta)

            File.WriteAllBytes("retencioncdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr))
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            Console.ReadLine()
        End Try
    End Sub

    Private Sub CrearDocumentoPercepcion()
        Try
            Console.WriteLine("Ejemplo de Percepción")
            Dim documento As New DocumentoPercepcion() With {
                .Emisor = CrearEmisor(),
                .Receptor = New Contribuyente() With {
                    .NroDocumento = "20100039207",
                    .TipoDocumento = "6",
                    .NombreLegal = "RANSA COMERCIAL S.A.",
                    .Ubigeo = "150101",
                    .Direccion = "Av. Argentina 2833",
                    .Urbanizacion = "-",
                    .Departamento = "CALLAO",
                    .Provincia = "CALLAO",
                    .Distrito = "CALLAO"
                },
                .IdDocumento = "P001-123",
                .FechaEmision = DateTime.Today.ToString(FormatoFecha),
                .Moneda = "PEN",
                .RegimenPercepcion = "01",
                .TasaPercepcion = 2,
                .ImporteTotalPercibido = 200,
                .ImporteTotalCobrado = 10000,
                .Observaciones = "Emision de Facturas del periodo Dic. 2016",
                .DocumentosRelacionados = New List(Of ItemPercepcion)() From {
                    New ItemPercepcion() With {
                        .NroDocumento = "E001-457",
                        .TipoDocumento = "01",
                        .MonedaDocumentoRelacionado = "USD",
                        .FechaEmision = DateTime.Today.AddDays(-3).ToString(FormatoFecha),
                        .ImporteTotal = 10000,
                        .FechaPago = DateTime.Today.ToString(FormatoFecha),
                        .NumeroPago = 153,
                        .ImporteSinPercepcion = 9800,
                        .ImportePercibido = 200,
                        .FechaPercepcion = DateTime.Today.ToString(FormatoFecha),
                        .ImporteTotalNeto = 10000,
                        .TipoCambio = 3.41D,
                        .FechaTipoCambio = DateTime.Today.ToString(FormatoFecha)
                    }
                }
            }

            Console.WriteLine("Generando XML....")

            Dim documentoResponse = RestHelper(Of DocumentoPercepcion, DocumentoResponse).Execute("GenerarPercepcion", documento)

            If Not documentoResponse.Exito Then
                Throw New InvalidOperationException(documentoResponse.MensajeError)
            End If

            Console.WriteLine("Firmando XML...")
            ' Firmado del Documento.
            Dim firmado As New FirmadoRequest() With {
                .TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                .CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                .PasswordCertificado = String.Empty,
                .UnSoloNodoExtension = True
            }

            Dim responseFirma = RestHelper(Of FirmadoRequest, FirmadoResponse).Execute("Firmar", firmado)

            If Not responseFirma.Exito Then
                Throw New InvalidOperationException(responseFirma.MensajeError)
            End If

            File.WriteAllBytes("percepcion.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado))

            Console.WriteLine("Enviando Retención a SUNAT....")

            Dim sendBill As New EnviarDocumentoRequest() With {
                .Ruc = documento.Emisor.NroDocumento,
                .UsuarioSol = "MODDATOS",
                .ClaveSol = "MODDATOS",
                .EndPointUrl = UrlOtroCpe,
                .IdDocumento = documento.IdDocumento,
                .TipoDocumento = "40",
                .TramaXmlFirmado = responseFirma.TramaXmlFirmado
            }

            Dim responseSendBill = RestHelper(Of EnviarDocumentoRequest, EnviarDocumentoResponse).Execute("EnviarDocumento", sendBill)

            If Not responseSendBill.Exito Then
                Throw New InvalidOperationException(responseSendBill.MensajeError)
            End If

            Console.WriteLine("Respuesta de SUNAT:")
            Console.WriteLine(responseSendBill.MensajeRespuesta)

            File.WriteAllBytes("percepcioncdr.zip", Convert.FromBase64String(responseSendBill.TramaZipCdr))
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            Console.ReadLine()
        End Try
    End Sub

End Module
