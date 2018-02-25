using System;
using System.Collections.Generic;
using System.IO;
using OpenInvoicePeru.Comun.Dto.Intercambio;
using OpenInvoicePeru.Comun.Dto.Modelos;

namespace OpenInvoicePeru.ApiClientCSharp
{
    class Program
    {
        private const string UrlSunat = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
        private const string UrlOtroCpe = "https://e-beta.sunat.gob.pe/ol-ti-itemision-otroscpe-gem-beta/billService";

        private const string FormatoFecha = "yyyy-MM-dd";
        private static string _ruc;

        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("Prueba de API REST de OpenInvoicePeru (C#)");
            Console.WriteLine("Ingrese su numero de RUC (puede elegir cualquiera)");
            _ruc = Console.ReadLine();
            CrearFactura();
            CrearBoleta();
            CrearNotaCredito();
            CrearResumenDiario();
            CrearComunicacionBaja();
            CrearDocumentoRetencion();
            CrearDocumentoPercepcion();

            Console.ReadLine();
        }

        private static Contribuyente CrearEmisor()
        {
            return new Contribuyente
            {
                NroDocumento = _ruc,
                TipoDocumento = "6",
                Direccion = "CAL.MORELLI NRO. 181 INT. P-2",
                Urbanizacion = "-",
                Departamento = "LIMA",
                Provincia = "LIMA",
                Distrito = "SAN BORJA",
                NombreComercial = "PLAZA VEA",
                NombreLegal = "SUPERMERCADOS PERUANOS SOCIEDAD ANONIMA",
                Ubigeo = "140101"
            };
        }

        private static void CrearFactura()
        {
            try
            {
                Console.WriteLine("Ejemplo Factura");
                var documento = new DocumentoElectronico
                {
                    Emisor = CrearEmisor(),
                    Receptor = new Contribuyente
                    {
                        NroDocumento = "20100039207",
                        TipoDocumento = "6",
                        NombreLegal = "RANSA COMERCIAL S.A."
                    },
                    IdDocumento = "FF11-001",
                    FechaEmision = DateTime.Today.AddDays(-5).ToString(FormatoFecha),
                    Moneda = "PEN",
                    MontoEnLetras = "SON CIENTO DIECIOCHO SOLES CON 0/100",
                    CalculoIgv = 0.18m,
                    CalculoIsc = 0.10m,
                    CalculoDetraccion = 0.04m,
                    TipoDocumento = "01",
                    TotalIgv = 18,
                    TotalVenta = 118,
                    Gravadas = 100,
                    Items = new List<DetalleDocumento>
                    {
                        new DetalleDocumento
                        {
                            Id = 1,
                            Cantidad = 5,
                            PrecioReferencial = 20,
                            PrecioUnitario = 20,
                            TipoPrecio = "01",
                            CodigoItem = "1234234",
                            Descripcion = "Arroz Costeño",
                            UnidadMedida = "KG",
                            Impuesto = 18,
                            TipoImpuesto = "10", // Gravada
                            TotalVenta = 100,
                            Suma = 100
                        }
                    }
                };

                Console.WriteLine("Generando XML....");

                var documentoResponse = RestHelper<DocumentoElectronico, DocumentoResponse>.Execute("GenerarFactura", documento);

                if (!documentoResponse.Exito)
                {
                    throw new InvalidOperationException(documentoResponse.MensajeError);
                }

                Console.WriteLine("Firmando XML...");
                // Firmado del Documento.
                var firmado = new FirmadoRequest
                {
                    TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                    PasswordCertificado = string.Empty,
                    UnSoloNodoExtension = false
                };

                var responseFirma = RestHelper<FirmadoRequest, FirmadoResponse>.Execute("Firmar", firmado);

                if (!responseFirma.Exito)
                {
                    throw new InvalidOperationException(responseFirma.MensajeError);
                }

                File.WriteAllBytes("factura.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado));

                Console.WriteLine("Enviando a SUNAT....");

                var documentoRequest = new EnviarDocumentoRequest
                {
                    Ruc = documento.Emisor.NroDocumento,
                    UsuarioSol = "MODDATOS",
                    ClaveSol = "MODDATOS",
                    EndPointUrl = UrlSunat,
                    IdDocumento = documento.IdDocumento,
                    TipoDocumento = documento.TipoDocumento,
                    TramaXmlFirmado = responseFirma.TramaXmlFirmado
                };

                var enviarDocumentoResponse = RestHelper<EnviarDocumentoRequest, EnviarDocumentoResponse>.Execute("EnviarDocumento", documentoRequest);

                if (!enviarDocumentoResponse.Exito)
                {
                    throw new InvalidOperationException(enviarDocumentoResponse.MensajeError);
                }

                File.WriteAllBytes("facturacdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr));

                Console.WriteLine("Respuesta de SUNAT:");
                Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }
        
        private static void CrearBoleta()
        {
            try
            {
                Console.WriteLine("Ejemplo Boleta");
                var documento = new DocumentoElectronico
                {
                    Emisor = CrearEmisor(),
                    Receptor = new Contribuyente
                    {
                        NroDocumento = "88888888",
                        TipoDocumento = "1",
                        NombreLegal = "CLIENTE GENERICO"
                    },
                    IdDocumento = "BB11-001",
                    FechaEmision = DateTime.Today.AddDays(-5).ToString(FormatoFecha),
                    Moneda = "PEN",
                    MontoEnLetras = "SON CIENTO DIECIOCHO SOLES CON 0/100",
                    CalculoIgv = 0.18m,
                    CalculoIsc = 0.10m,
                    CalculoDetraccion = 0.04m,
                    TipoDocumento = "03",
                    TotalIgv = 18,
                    TotalVenta = 118,
                    Gravadas = 100,
                    Items = new List<DetalleDocumento>
                    {
                        new DetalleDocumento
                        {
                            Id = 1,
                            Cantidad = 10,
                            PrecioReferencial = 10,
                            PrecioUnitario = 10,
                            TipoPrecio = "01",
                            CodigoItem = "2435675",
                            Descripcion = "USB Kingston ©",
                            UnidadMedida = "NIU",
                            Impuesto = 18,
                            TipoImpuesto = "10", // Gravada
                            TotalVenta = 100,
                            Suma = 100
                        }
                    }
                };

                Console.WriteLine("Generando XML....");

                var documentoResponse = RestHelper<DocumentoElectronico, DocumentoResponse>.Execute("GenerarFactura", documento);

                if (!documentoResponse.Exito)
                {
                    throw new InvalidOperationException(documentoResponse.MensajeError);
                }

                Console.WriteLine("Firmando XML...");
                // Firmado del Documento.
                var firmado = new FirmadoRequest
                {
                    TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                    PasswordCertificado = string.Empty,
                    UnSoloNodoExtension = false
                };

                var responseFirma = RestHelper<FirmadoRequest, FirmadoResponse>.Execute("Firmar", firmado);

                if (!responseFirma.Exito)
                {
                    throw new InvalidOperationException(responseFirma.MensajeError);
                }

                File.WriteAllBytes("boleta.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado));

                Console.WriteLine("Enviando a SUNAT....");

                var documentoRequest = new EnviarDocumentoRequest
                {
                    Ruc = documento.Emisor.NroDocumento,
                    UsuarioSol = "MODDATOS",
                    ClaveSol = "MODDATOS",
                    EndPointUrl = UrlSunat,
                    IdDocumento = documento.IdDocumento,
                    TipoDocumento = documento.TipoDocumento,
                    TramaXmlFirmado = responseFirma.TramaXmlFirmado
                };

                var enviarDocumentoResponse = RestHelper<EnviarDocumentoRequest, EnviarDocumentoResponse>.Execute("EnviarDocumento", documentoRequest);

                if (!enviarDocumentoResponse.Exito)
                {
                    throw new InvalidOperationException(enviarDocumentoResponse.MensajeError);
                }

                File.WriteAllBytes("boletacdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr));

                Console.WriteLine("Respuesta de SUNAT:");
                Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }
        
        private static void CrearNotaCredito()
        {
            try
            {
                Console.WriteLine("Ejemplo Nota de Crédito de Factura");
                var documento = new DocumentoElectronico
                {
                    Emisor = CrearEmisor(),
                    Receptor = new Contribuyente
                    {
                        NroDocumento = "20257471609",
                        TipoDocumento = "6",
                        NombreLegal = "FRAMEWORK PERU"
                    },
                    IdDocumento = "FN11-001",
                    FechaEmision = DateTime.Today.AddDays(-5).ToString(FormatoFecha),
                    Moneda = "PEN",
                    MontoEnLetras = "SON CINCO SOLES CON 0/100",
                    CalculoIgv = 0.18m,
                    CalculoIsc = 0.10m,
                    CalculoDetraccion = 0.04m,
                    TipoDocumento = "07",
                    TotalIgv = 0.76m,
                    TotalVenta = 5,
                    Gravadas = 4.24m,
                    Items = new List<DetalleDocumento>
                    {
                        new DetalleDocumento
                        {
                            Id = 1,
                            Cantidad = 1,
                            PrecioReferencial = 4.24m,
                            PrecioUnitario = 4.24m,
                            TipoPrecio = "01",
                            CodigoItem = "2435675",
                            Descripcion = "Correcion Factura",
                            UnidadMedida = "NIU",
                            Impuesto = 0.76m,
                            TipoImpuesto = "10", // Gravada
                            TotalVenta = 5,
                            Suma = 5
                        }
                    },
                    Discrepancias = new List<Discrepancia>
                    {
                        new Discrepancia
                        {
                            NroReferencia = "FF11-001",
                            Tipo = "01",
                            Descripcion = "Anulacion de la operacion"
                        }
                    },
                    Relacionados = new List<DocumentoRelacionado>
                    {
                        new DocumentoRelacionado
                        {
                            NroDocumento = "FF11-001",
                            TipoDocumento = "01"
                        }
                    }
                };

                Console.WriteLine("Generando XML....");

                var documentoResponse = RestHelper<DocumentoElectronico, DocumentoResponse>.Execute("GenerarNotaCredito", documento);

                if (!documentoResponse.Exito)
                {
                    throw new InvalidOperationException(documentoResponse.MensajeError);
                }

                Console.WriteLine("Firmando XML...");
                // Firmado del Documento.
                var firmado = new FirmadoRequest
                {
                    TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                    PasswordCertificado = string.Empty,
                    UnSoloNodoExtension = false
                };

                var responseFirma = RestHelper<FirmadoRequest, FirmadoResponse>.Execute("Firmar", firmado);

                if (!responseFirma.Exito)
                {
                    throw new InvalidOperationException(responseFirma.MensajeError);
                }

                File.WriteAllBytes("notacredito.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado));

                Console.WriteLine("Enviando a SUNAT....");

                var documentoRequest = new EnviarDocumentoRequest
                {
                    Ruc = documento.Emisor.NroDocumento,
                    UsuarioSol = "MODDATOS",
                    ClaveSol = "MODDATOS",
                    EndPointUrl = UrlSunat,
                    IdDocumento = documento.IdDocumento,
                    TipoDocumento = documento.TipoDocumento,
                    TramaXmlFirmado = responseFirma.TramaXmlFirmado
                };

                var enviarDocumentoResponse = RestHelper<EnviarDocumentoRequest, EnviarDocumentoResponse>.Execute("EnviarDocumento", documentoRequest);

                if (!enviarDocumentoResponse.Exito)
                {
                    throw new InvalidOperationException(enviarDocumentoResponse.MensajeError);
                }

                File.WriteAllBytes("notacreditocdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr));

                Console.WriteLine("Respuesta de SUNAT:");
                Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        private static void CrearResumenDiario()
        {
            try
            {
                Console.WriteLine("Ejemplo de Resumen Diario");
                var documentoResumenDiario = new ResumenDiarioNuevo
                {
                    IdDocumento = string.Format("RC-{0:yyyyMMdd}-001", DateTime.Today),
                    FechaEmision = DateTime.Today.ToString(FormatoFecha),
                    FechaReferencia = DateTime.Today.AddDays(-1).ToString(FormatoFecha),
                    Emisor = CrearEmisor(),
                    Resumenes = new List<GrupoResumenNuevo>()
                };

                documentoResumenDiario.Resumenes.Add(new GrupoResumenNuevo
                {
                    Id = 1,
                    TipoDocumento = "03",
                    IdDocumento = "BB14-33386",
                    NroDocumentoReceptor = "41614074",
                    TipoDocumentoReceptor = "1",
                    CodigoEstadoItem = 1, // 1 - Agregar. 2 - Modificar. 3 - Eliminar
                    Moneda = "PEN",
                    TotalVenta = 190.9m,
                    TotalIgv = 29.12m,
                    Gravadas = 161.78m,
                });
                // Para los casos de envio de boletas anuladas, se debe primero informar las boletas creadas (1) y luego en un segundo resumen se envian las anuladas. De lo contrario se presentará el error 'El documento indicado no existe no puede ser modificado/eliminado'
                documentoResumenDiario.Resumenes.Add(new GrupoResumenNuevo
                {
                    Id = 2,
                    TipoDocumento = "03",
                    IdDocumento = "BB30-33384",
                    NroDocumentoReceptor = "08506678",
                    TipoDocumentoReceptor = "1",
                    CodigoEstadoItem = 1, // 1 - Agregar. 2 - Modificar. 3 - Eliminar
                    Moneda = "USD",
                    TotalVenta = 9580m,
                    TotalIgv = 1411.36m,
                    Gravadas = 8168.64m,
                });


                Console.WriteLine("Generando XML....");
                
                var documentoResponse = RestHelper<ResumenDiarioNuevo, DocumentoResponse>.Execute("GenerarResumenDiario/v2", documentoResumenDiario);

                if (!documentoResponse.Exito)
                    throw new InvalidOperationException(documentoResponse.MensajeError);

                Console.WriteLine("Firmando XML...");
                // Firmado del Documento.
                var firmado = new FirmadoRequest
                {
                    TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("Certificado.pfx")),
                    PasswordCertificado = string.Empty,
                    UnSoloNodoExtension = true
                };

                var responseFirma = RestHelper<FirmadoRequest, FirmadoResponse>.Execute("Firmar", firmado);

                if (!responseFirma.Exito)
                {
                    throw new InvalidOperationException(responseFirma.MensajeError);
                }

                Console.WriteLine("Guardando XML de Resumen....(Revisar carpeta del ejecutable)");

                File.WriteAllBytes("resumendiario.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado));

                Console.WriteLine("Enviando a SUNAT....");

                var enviarDocumentoRequest = new EnviarDocumentoRequest
                {
                    Ruc = documentoResumenDiario.Emisor.NroDocumento,
                    UsuarioSol = "MODDATOS",
                    ClaveSol = "MODDATOS",
                    EndPointUrl = UrlSunat,
                    IdDocumento = documentoResumenDiario.IdDocumento,
                    TramaXmlFirmado = responseFirma.TramaXmlFirmado
                };

                var enviarResumenResponse = RestHelper<EnviarDocumentoRequest, EnviarResumenResponse>.Execute("EnviarResumen", enviarDocumentoRequest);

                if (!enviarResumenResponse.Exito)
                {
                    throw new InvalidOperationException(enviarResumenResponse.MensajeError);
                }

                Console.WriteLine("Nro de Ticket: {0}", enviarResumenResponse.NroTicket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        private static void CrearComunicacionBaja()
        {
            try
            {
                Console.WriteLine("Ejemplo de Comunicación de Baja");
                var documentoBaja = new ComunicacionBaja
                {
                    IdDocumento = string.Format("RA-{0:yyyyMMdd}-001", DateTime.Today),
                    FechaEmision = DateTime.Today.ToString(FormatoFecha),
                    FechaReferencia = DateTime.Today.AddDays(-1).ToString(FormatoFecha),
                    Emisor = CrearEmisor(),
                    Bajas = new List<DocumentoBaja>()
                };

                // En las comunicaciones de Baja ya no se pueden colocar boletas, ya que la anulacion de las mismas
                // la realiza el resumen diario.
                documentoBaja.Bajas.Add(new DocumentoBaja
                {
                    Id = 1,
                    Correlativo = "33386",
                    TipoDocumento = "01",
                    Serie = "FA50",
                    MotivoBaja = "Anulación por otro tipo de documento"
                });
                documentoBaja.Bajas.Add(new DocumentoBaja
                {
                    Id = 2,
                    Correlativo = "86486",
                    TipoDocumento = "01",
                    Serie = "FF14",
                    MotivoBaja = "Anulación por otro datos erroneos"
                });

                Console.WriteLine("Generando XML....");
                
                var documentoResponse = RestHelper<ComunicacionBaja, DocumentoResponse>.Execute("GenerarComunicacionBaja", documentoBaja);
                if (!documentoResponse.Exito)
                {
                    throw new InvalidOperationException(documentoResponse.MensajeError);
                }

                Console.WriteLine("Firmando XML...");
                // Firmado del Documento.
                var firmado = new FirmadoRequest
                {
                    TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("Certificado.pfx")),
                    PasswordCertificado = string.Empty,
                    UnSoloNodoExtension = true
                };

                var responseFirma = RestHelper<FirmadoRequest, FirmadoResponse>.Execute("Firmar", firmado);

                if (!responseFirma.Exito)
                {
                    throw new InvalidOperationException(responseFirma.MensajeError);
                }

                Console.WriteLine("Guardando XML de la Comunicacion de Baja....(Revisar carpeta del ejecutable)");

                File.WriteAllBytes("comunicacionbaja.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado));

                Console.WriteLine("Enviando a SUNAT....");

                var sendBill = new EnviarDocumentoRequest
                {
                    Ruc = documentoBaja.Emisor.NroDocumento,
                    UsuarioSol = "MODDATOS",
                    ClaveSol = "MODDATOS",
                    EndPointUrl = UrlSunat,
                    IdDocumento = documentoBaja.IdDocumento,
                    TramaXmlFirmado = responseFirma.TramaXmlFirmado
                };

                var enviarResumenResponse = RestHelper<EnviarDocumentoRequest, EnviarResumenResponse>.Execute("EnviarResumen", sendBill);

                if (!enviarResumenResponse.Exito)
                {
                    throw new InvalidOperationException(enviarResumenResponse.MensajeError);
                }

                Console.WriteLine("Nro de Ticket: {0}", enviarResumenResponse.NroTicket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        private static void CrearDocumentoRetencion()
        {
            try
            {
                Console.WriteLine("Ejemplo de Retención");
                var documento = new DocumentoRetencion
                {
                    Emisor = CrearEmisor(),
                    Receptor = new Contribuyente
                    {
                        NroDocumento = "20100039207",
                        TipoDocumento = "6",
                        NombreLegal = "RANSA COMERCIAL S.A.",
                        Ubigeo = "150101",
                        Direccion = "Av. Argentina 2833",
                        Urbanizacion = "-",
                        Departamento = "CALLAO",
                        Provincia = "CALLAO",
                        Distrito = "CALLAO"
                    },
                    IdDocumento = "R001-123",
                    FechaEmision = DateTime.Today.ToString(FormatoFecha),
                    Moneda = "PEN",
                    RegimenRetencion = "01",
                    TasaRetencion = 3,
                    ImporteTotalRetenido = 300,
                    ImporteTotalPagado = 10000,
                    Observaciones = "Emision de Facturas del periodo Dic. 2016",
                    DocumentosRelacionados = new List<ItemRetencion>
                    {
                        new ItemRetencion
                        {
                            NroDocumento = "E001-457",
                            TipoDocumento = "01",
                            MonedaDocumentoRelacionado = "USD",
                            FechaEmision = DateTime.Today.AddDays(-3).ToString(FormatoFecha),
                            ImporteTotal = 10000,
                            FechaPago = DateTime.Today.ToString(FormatoFecha),
                            NumeroPago = 153,
                            ImporteSinRetencion = 9700,
                            ImporteRetenido = 300,
                            FechaRetencion = DateTime.Today.ToString(FormatoFecha),
                            ImporteTotalNeto = 10000,
                            TipoCambio = 3.41m,
                            FechaTipoCambio = DateTime.Today.ToString(FormatoFecha)
                        }
                    }
                };

                Console.WriteLine("Generando XML....");

                var documentoResponse = RestHelper<DocumentoRetencion, DocumentoResponse>.Execute("GenerarRetencion", documento);

                if (!documentoResponse.Exito)
                {
                    throw new InvalidOperationException(documentoResponse.MensajeError);
                }

                Console.WriteLine("Firmando XML...");
                // Firmado del Documento.
                var firmado = new FirmadoRequest
                {
                    TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                    PasswordCertificado = string.Empty,
                    UnSoloNodoExtension = true
                };

                var responseFirma = RestHelper<FirmadoRequest, FirmadoResponse>.Execute("Firmar", firmado);

                if (!responseFirma.Exito)
                {
                    throw new InvalidOperationException(responseFirma.MensajeError);
                }

                File.WriteAllBytes("retencion.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado));

                Console.WriteLine("Enviando Retención a SUNAT....");

                var enviarDocumentoRequest = new EnviarDocumentoRequest
                {
                    Ruc = documento.Emisor.NroDocumento,
                    UsuarioSol = "MODDATOS",
                    ClaveSol = "MODDATOS",
                    EndPointUrl = UrlOtroCpe,
                    IdDocumento = documento.IdDocumento,
                    TipoDocumento = "20",
                    TramaXmlFirmado = responseFirma.TramaXmlFirmado
                };

                var enviarDocumentoResponse = RestHelper<EnviarDocumentoRequest, EnviarDocumentoResponse>.Execute("EnviarDocumento", enviarDocumentoRequest);

                if (!enviarDocumentoResponse.Exito)
                {
                    throw new InvalidOperationException(enviarDocumentoResponse.MensajeError);
                }

                Console.WriteLine("Respuesta de SUNAT:");
                Console.WriteLine(enviarDocumentoResponse.MensajeRespuesta);

                File.WriteAllBytes("retencioncdr.zip", Convert.FromBase64String(enviarDocumentoResponse.TramaZipCdr));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        private static void CrearDocumentoPercepcion()
        {
            try
            {
                Console.WriteLine("Ejemplo de Percepción");
                var documento = new DocumentoPercepcion
                {
                    Emisor = CrearEmisor(),
                    Receptor = new Contribuyente
                    {
                        NroDocumento = "20100039207",
                        TipoDocumento = "6",
                        NombreLegal = "RANSA COMERCIAL S.A.",
                        Ubigeo = "150101",
                        Direccion = "Av. Argentina 2833",
                        Urbanizacion = "-",
                        Departamento = "CALLAO",
                        Provincia = "CALLAO",
                        Distrito = "CALLAO"
                    },
                    IdDocumento = "P001-123",
                    FechaEmision = DateTime.Today.ToString(FormatoFecha),
                    Moneda = "PEN",
                    RegimenPercepcion = "01",
                    TasaPercepcion = 2,
                    ImporteTotalPercibido = 200,
                    ImporteTotalCobrado = 10000,
                    Observaciones = "Emision de Facturas del periodo Dic. 2016",
                    DocumentosRelacionados = new List<ItemPercepcion>
                    {
                        new ItemPercepcion
                        {
                            NroDocumento = "E001-457",
                            TipoDocumento = "01",
                            MonedaDocumentoRelacionado = "USD",
                            FechaEmision = DateTime.Today.AddDays(-3).ToString(FormatoFecha),
                            ImporteTotal = 10000,
                            FechaPago = DateTime.Today.ToString(FormatoFecha),
                            NumeroPago = 153,
                            ImporteSinPercepcion = 9800,
                            ImportePercibido = 200,
                            FechaPercepcion = DateTime.Today.ToString(FormatoFecha),
                            ImporteTotalNeto = 10000,
                            TipoCambio = 3.41m,
                            FechaTipoCambio = DateTime.Today.ToString(FormatoFecha)
                        }
                    }
                };

                Console.WriteLine("Generando XML....");

                var documentoResponse = RestHelper<DocumentoPercepcion, DocumentoResponse>.Execute("GenerarPercepcion", documento);

                if (!documentoResponse.Exito)
                {
                    throw new InvalidOperationException(documentoResponse.MensajeError);
                }

                Console.WriteLine("Firmando XML...");
                // Firmado del Documento.
                var firmado = new FirmadoRequest
                {
                    TramaXmlSinFirma = documentoResponse.TramaXmlSinFirma,
                    CertificadoDigital = Convert.ToBase64String(File.ReadAllBytes("certificado.pfx")),
                    PasswordCertificado = string.Empty,
                    UnSoloNodoExtension = true
                };

                var responseFirma = RestHelper<FirmadoRequest, FirmadoResponse>.Execute("Firmar", firmado);

                if (!responseFirma.Exito)
                {
                    throw new InvalidOperationException(responseFirma.MensajeError);
                }

                File.WriteAllBytes("percepcion.xml", Convert.FromBase64String(responseFirma.TramaXmlFirmado));

                Console.WriteLine("Enviando Retención a SUNAT....");

                var sendBill = new EnviarDocumentoRequest
                {
                    Ruc = documento.Emisor.NroDocumento,
                    UsuarioSol = "MODDATOS",
                    ClaveSol = "MODDATOS",
                    EndPointUrl = UrlOtroCpe,
                    IdDocumento = documento.IdDocumento,
                    TipoDocumento = "40",
                    TramaXmlFirmado = responseFirma.TramaXmlFirmado
                };

                var responseSendBill = RestHelper<EnviarDocumentoRequest, EnviarDocumentoResponse>.Execute("EnviarDocumento", sendBill);

                if (!responseSendBill.Exito)
                {
                    throw new InvalidOperationException(responseSendBill.MensajeError);
                }

                Console.WriteLine("Respuesta de SUNAT:");
                Console.WriteLine(responseSendBill.MensajeRespuesta);

                File.WriteAllBytes("percepcioncdr.zip", Convert.FromBase64String(responseSendBill.TramaZipCdr));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
