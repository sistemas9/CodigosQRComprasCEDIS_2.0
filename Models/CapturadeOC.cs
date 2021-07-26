using CodigosQRComprasCEDIS_2._0.Helpers;
using CodigosQRComprasCEDIS_2._0.TestHelper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using RestSharp;
using RestSharp.Authenticators;

namespace CodigosQRComprasCEDIS_2._0.Models
{
    public class CapturadeOC
    {
        private DatabaseAYT03 dbayt03 = new DatabaseAYT03();
        private Database dbprod = new Database();
        public String config { get; set; }
        public String company { get; set; }
        public GetConfigsData configs { get; set; }
        public String urlRestClient { get; set; }
        public String urlCodigosQRPDF { get; set; }

        public CapturadeOC()
        {
            ////////////Obtener datos de configuracion////////////////////////////////////////////////////////////////////////////////////
            this.configs = new GetConfigsData();
            this.config = configs.GetConfigurationData("Config").Result;
            this.company = configs.GetConfigurationData("Company").Result;
            if (this.company == "atp")
            {
                if (config == "DESARROLLO")
                {
                    urlRestClient = this.configs.GetConfigurationData("URL_RESTCLIENT_TES").Result;
                    urlCodigosQRPDF = configs.GetConfigurationData("URL_CODIGOSQR_PDF_TES").Result;
                }
                else
                {
                    urlRestClient = this.configs.GetConfigurationData("URL_RESTCLIENT_PROD").Result;
                    urlCodigosQRPDF = configs.GetConfigurationData("URL_CODIGOSQR_PDF_TES").Result;
                }
            }
        }

        public async Task<ResponseOrdenCompra> GetDataOC(String OC)
        {
            try
            {
                AuthenticationHeader authentication = new AuthenticationHeader();
                String Token = await authentication.getAuthenticationHeader();
                ////////////////////////////////////////////////////consulta entity///////////////////////////////////////////////////////////////
                ResponseOrdenCompra ordenCompra = new ResponseOrdenCompra();
                var client = new RestClient(this.urlRestClient);
                client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(Token, "Bearer");

                var request = new RestRequest("PurchaseOrderHeadersV2?$filter=PurchaseOrderNumber%20eq%20'" + OC + "'&$select=PurchaseOrderNumber%2CPurchaseOrderName%2CRequestedDeliveryDate%2CBuyerGroupId%2CCurrencyCode%2CDefaultReceivingWarehouseId%2CDefaultReceivingSiteId", Method.GET);
                request.AddHeader("Accept", "application/json;");
                request.AddHeader("Body", "");
                request.AddCookie(".AspNetCore.Antiforgery.XT6nEUiSeek", "CfDJ8AhqgK3czbJLjqzYhQiMaH_TrBZZcCf-eQ74T813xl-VDkXZYIFNZLxEQ_M9_bLJ8do1-ogXgQqPwswclht26EPwi3FyxUzjKgQphPezPU9_oztW9btiRoEO21kv7tALRpDivWdpXNWpeU1UvwmV11Q");
                String returnedStr = client.Execute(request).Content;
                returnedStr = returnedStr.Replace("@odata", "");

                ordenCompra = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseOrdenCompra>(returnedStr);
                DateTime RequestedDeliveryDate = Convert.ToDateTime(ordenCompra.value[0].RequestedDeliveryDate);
                ordenCompra.value[0].RequestedDeliveryDate = RequestedDeliveryDate.ToString("dd/MM/yyyy");
                ordenCompra.value[0].RequestedDeliveryDateOrig = RequestedDeliveryDate.ToString("yyyy-MM-dd");
                return ordenCompra;
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return new ResponseOrdenCompra();
            }
            
        }

        public async Task<List<String[]>> GetDataOCLinea(String OC)
        {
            AuthenticationHeader authentication = new AuthenticationHeader();
            String Token = await authentication.getAuthenticationHeader();
            ////////////////////////////////////////////////////consulta entity///////////////////////////////////////////////////////////////
            var client = new RestClient(this.urlRestClient);
            client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(Token, "Bearer");

            var request = new RestRequest("PurchaseOrderLines?$filter=PurchaseOrderNumber%20eq%20'" + OC + "'&$select=LineNumber%2CItemNumber%2CPurchaseOrderNumber%2CLineDescription%2COrderedPurchaseQuantity%2CPurchaseUnitSymbol", Method.GET);
            request.AddHeader("Accept", "application/json;");
            request.AddHeader("Body", "");
            request.AddCookie(".AspNetCore.Antiforgery.XT6nEUiSeek", "CfDJ8AhqgK3czbJLjqzYhQiMaH_TrBZZcCf-eQ74T813xl-VDkXZYIFNZLxEQ_M9_bLJ8do1-ogXgQqPwswclht26EPwi3FyxUzjKgQphPezPU9_oztW9btiRoEO21kv7tALRpDivWdpXNWpeU1UvwmV11Q");
            String returnedStr = client.Execute(request).Content;
            returnedStr = returnedStr.Replace("@odata", "");

            ResponseLineasOC ocLineaResp = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseLineasOC>(returnedStr);

            List<String[]> ocLinea = new List<String[]>();
            int status = 0;
            String estilo = "", itemNumber = "", cant = "", recibido = "", lineDescExternal = "";
            List<bool> flagOrdenCompleta = new List<bool>();
            bool parcial = false;
            var packages = ocLineaResp.value;
            if (packages.Count() > 0)
            {
                foreach (var ocData in packages)
                {
                    SqlDataReader reader;
                    String query = "SELECT status,itemNumber,orderedPurchaseQuantity,recibido,sinLote FROM AYT_CodigosQROrdenCompraLineas WHERE purchaseOrderNumber = '" + OC + "' AND itemNumber = '" + ocData.ItemNumber.ToString() + "';";
                    if (config != "DESARROLLO")
                    {
                        reader = (SqlDataReader)await dbayt03.Query(query);
                    }
                    else
                    {
                        reader = (SqlDataReader)await dbprod.Query(query);
                    }
                    bool noIngresada = false;
                    Double orderedPurchaseQuantity = 0;
                    if (reader.HasRows)
                    {
                        
                        while (reader.Read())
                        {
                            status = Convert.ToInt32(reader[0].ToString());
                            itemNumber = reader[1].ToString();
                            orderedPurchaseQuantity += Convert.ToDouble(reader[2].ToString());
                            recibido = reader[3].ToString();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                        noIngresada = true;
                    }
                    reader.Close();
                    /////////////////el codigo externo del articulo//////////////////////////////////////////////
                    lineDescExternal = ocData.LineDescription;
                    var clientExternal = new RestClient(urlRestClient);
                    clientExternal.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(Token, "Bearer");
                    var requestExternal = new RestRequest("VendorProductDescriptions?$filter=ItemNumber%20eq%20'" + ocData.ItemNumber.ToString() + "'&$select=VendorProductDescription", Method.GET);
                    requestExternal.AddHeader("Accept", "application/json;");
                    requestExternal.AddHeader("Body", "");
                    request.AddCookie(".AspNetCore.Antiforgery.XT6nEUiSeek", "CfDJ8AhqgK3czbJLjqzYhQiMaH_TrBZZcCf-eQ74T813xl-VDkXZYIFNZLxEQ_M9_bLJ8do1-ogXgQqPwswclht26EPwi3FyxUzjKgQphPezPU9_oztW9btiRoEO21kv7tALRpDivWdpXNWpeU1UvwmV11Q");
                    String returnedStrExternal = client.Execute(requestExternal).Content;
                    returnedStrExternal = returnedStrExternal.Replace("@odata", "");

                    ResponseNombreExterno nombreExterno = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseNombreExterno>(returnedStrExternal);
                    var packagesExternal = nombreExterno.value;
                    if (packagesExternal.Count() > 0)
                    {
                        foreach (var dataExt in packagesExternal)
                        {
                            lineDescExternal = dataExt.VendorProductDescription;
                            lineDescExternal = lineDescExternal.Replace("'", "ft. ");
                            lineDescExternal = lineDescExternal.Replace("\"", "inch. ");
                        }
                    }
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    switch (status)
                    {
                        case 1:
                            estilo = " ";
                            cant = ocData.OrderedPurchaseQuantity.ToString();
                        break;
                        case 2:
                            estilo = "style=\"border:solid 1px red\"";
                            Decimal purQuan = Convert.ToDecimal(ocData.OrderedPurchaseQuantity);
                            Decimal dbQuan = Convert.ToDecimal(orderedPurchaseQuantity);
                            cant = (purQuan - dbQuan).ToString();
                            parcial = true;
                        break;
                        case 3:
                            estilo = "disabled";
                            cant = ocData.OrderedPurchaseQuantity.ToString();
                        break;
                        default:
                            estilo = " ";
                            cant = ocData.OrderedPurchaseQuantity.ToString();
                        break;
                    }
                    if (status == 3 && recibido == "1")
                    {
                        flagOrdenCompleta.Add(true);
                    }
                    else
                    {
                        flagOrdenCompleta.Add(false);
                    }
                    String LineDesc = ocData.LineDescription.Replace("'", "ft. ");
                    LineDesc = LineDesc.Replace("\"", "inch. ");
                    String[] arr = new string[] {
                                ocData.LineNumber.ToString(),
                                ocData.ItemNumber.ToString(),
                                lineDescExternal,
                                "<input "+estilo+" type=\"number\" value=\""+cant+"\" max=\""+cant+"\" min=\"1\" class=\"form-control canti\" oninput=\"if (Number(this.value) > Number(this.max)) this.value = this.max\" data-cantdyna=\""+ocData.OrderedPurchaseQuantity.ToString()+"\" data-parcial=\""+parcial.ToString()+"\">",
                                ocData.PurchaseUnitSymbol,
                                "<label for=\"chkSeleccionar\" style=\"font-size:10px;\">Seleccionar</label><input type=\"checkbox\" class=\"form-control chkSeleccionar\" style=\"width:20px;height:20px;\" onclick=\"checarRow(this)\"/>",
                                "<label for=\"chkSinLote\" style=\"font-size:10px;\">Sin Lote</label><input type=\"checkbox\" class=\"form-control chkSinLote\" style=\"width:20px;height:20px;\" />",
                                "<label for=\"chkSeries\" style=\"font-size:10px;\">Series</label><input type=\"checkbox\" class=\"form-control chkSeries\" style=\"width:20px;height:20px;\" onclick=\"checarRowSerie(this)\"/>",
                                LineDesc
                            };
                    if (status != 3 || noIngresada)
                    {
                        ocLinea.Add(arr);
                    }
                    status = 0;
                    parcial = false;
                }
            }
            return ocLinea;
        }

        public async Task<List<String[]>> GetDataOCLineaCodigos(String OC, String ocRevision, String revision)
        {
            AuthenticationHeader authentication = new AuthenticationHeader();
            String Token = await authentication.getAuthenticationHeader();
            ////////////////////////////////////////////////////consulta entity///////////////////////////////////////////////////////////////
            List<String[]> ocLinea = new List<String[]>();
            var client = new RestClient(this.urlRestClient);
            client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(Token, "Bearer");
            var request = new RestRequest("PurchaseOrderLines?$filter=PurchaseOrderNumber%20eq%20'" + OC + "'&$select=LineNumber%2CPurchaseOrderNumber%2CItemNumber%2CLineDescription%2COrderedPurchaseQuantity%2CPurchaseUnitSymbol", Method.GET);
            request.AddHeader("Accept", "application/json;");
            request.AddHeader("Body", "");
            request.AddCookie(".AspNetCore.Antiforgery.XT6nEUiSeek", "CfDJ8AhqgK3czbJLjqzYhQiMaH_TrBZZcCf-eQ74T813xl-VDkXZYIFNZLxEQ_M9_bLJ8do1-ogXgQqPwswclht26EPwi3FyxUzjKgQphPezPU9_oztW9btiRoEO21kv7tALRpDivWdpXNWpeU1UvwmV11Q");
            String returnedStr = client.Execute(request).Content;
            returnedStr = returnedStr.Replace("@odata", "");
            ResponseLineasOC ocLineaResp = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseLineasOC>(returnedStr);
            var packages = ocLineaResp.value;

            int status = 0;
            String estilo = "", itemNumber = "", cant = "", recibido = "", lineDescExternal = "", sinLote = "";
            List<bool> flagOrdenCompleta = new List<bool>();
            bool parcial = false;
            if (packages.Count() > 0)
            {
                foreach (var ocData in packages)
                {
                    SqlDataReader reader;
                    //String query = "SELECT status,itemNumber,orderedPurchaseQuantity,recibido,sinLote FROM AYT_CodigosQROrdenCompraLineas WHERE purchaseOrderNumber = '" + OC + "' AND itemNumber = '" + ocData.ItemNumber.ToString() + "' AND recibido = 1 AND revision = " + revision + ";";
                    String query = "SELECT	status ";
                    query += "		,itemNumber ";
                    query += "		,IIF(hasSubRevision != 1,orderedPurchaseQuantity,(SELECT orderedPurchaseQuantity  ";
                    query += "														FROM AYT_CodigosQRSubRevisionLinea  ";
                    query += "														WHERE purchaseOrderNumber = '" + OC + "'  ";
                    query += "														AND itemNumber = '" + ocData.ItemNumber.ToString() + "'  ";
                    query += "														AND revision = " + revision + ")) AS orderedPurchaseQuantity ";
                    query += "		,recibido ";
                    query += "		,sinLote  ";
                    query += "FROM AYT_CodigosQROrdenCompraLineas  ";
                    query += "WHERE purchaseOrderNumber = '" + OC + "'  ";
                    query += "AND itemNumber = '" + ocData.ItemNumber.ToString() + "'  ";
                    query += "AND recibido = 1  ";
                    query += "AND revision = " + revision + " ";
                    query += "OR ( hasSubRevision = 1 AND itemNumber = '" + ocData.ItemNumber.ToString() + "' ); ";
                    if (config != "DESARROLLO")
                    {
                        reader = (SqlDataReader)await dbayt03.Query(query);
                    }
                    else
                    {
                        reader = (SqlDataReader)await dbprod.Query(query);
                    }
                    bool noIngresada = false;
                    Double orderedPurchaseQuantity = 0;
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            status = Convert.ToInt32(reader[0].ToString());
                            itemNumber = reader[1].ToString();
                            orderedPurchaseQuantity += Convert.ToDouble(reader[2].ToString());
                            recibido = reader[3].ToString();
                            sinLote = reader[4].ToString();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                        noIngresada = true;
                    }
                    reader.Close();
                    /////////////////el codigo externo del articulo//////////////////////////////////////////////
                    lineDescExternal = ocData.LineDescription;
                    var clientExternal = new RestClient(this.urlRestClient);
                    clientExternal.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(Token, "Bearer");
                    var requestExternal = new RestRequest("VendorProductDescriptions?$filter=ItemNumber%20eq%20'" + ocData.ItemNumber.ToString() + "'&$select=VendorProductDescription", Method.GET);
                    requestExternal.AddHeader("Accept", "application/json;");
                    requestExternal.AddHeader("Body", "");
                    request.AddCookie(".AspNetCore.Antiforgery.XT6nEUiSeek", "CfDJ8AhqgK3czbJLjqzYhQiMaH_TrBZZcCf-eQ74T813xl-VDkXZYIFNZLxEQ_M9_bLJ8do1-ogXgQqPwswclht26EPwi3FyxUzjKgQphPezPU9_oztW9btiRoEO21kv7tALRpDivWdpXNWpeU1UvwmV11Q");
                    String returnedStrExternal = client.Execute(requestExternal).Content;
                    returnedStrExternal = returnedStrExternal.Replace("@odata", "");

                    ResponseNombreExterno nombreExterno = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseNombreExterno>(returnedStrExternal);
                    var packagesExternal = nombreExterno.value;
                    if (packagesExternal.Count() > 0)
                    {
                        foreach (var dataExt in packagesExternal)
                        {
                            lineDescExternal = dataExt.VendorProductDescription;
                            lineDescExternal = lineDescExternal.Replace("'", "ft. ");
                            lineDescExternal = lineDescExternal.Replace("\"", "inch. ");
                        }
                    }
                    //////////////////////////////////////////////////////////////////////////////////////////////
                    switch (status)
                    {
                        case 1:
                            estilo = " ";
                            cant = ocData.OrderedPurchaseQuantity.ToString();
                            break;
                        case 2:
                            estilo = "style=\"border:solid 1px red\" disabled";
                            Decimal purQuan = Convert.ToDecimal(ocData.OrderedPurchaseQuantity);
                            Decimal dbQuan = Convert.ToDecimal(orderedPurchaseQuantity);
                            cant = (purQuan - dbQuan).ToString();
                            parcial = true;
                            break;
                        case 3:
                            estilo = "disabled";
                            cant = ocData.OrderedPurchaseQuantity.ToString();
                            break;
                        default:
                            estilo = " ";
                            cant = ocData.OrderedPurchaseQuantity.ToString();
                            break;
                    }
                    if (status == 3 && recibido == "1")
                    {
                        flagOrdenCompleta.Add(true);
                    }
                    else
                    {
                        flagOrdenCompleta.Add(false);
                    }
                    String LineDesc = ocData.LineDescription.Replace("'", "ft. ");
                    LineDesc = LineDesc.Replace("\"", "inch. ");
                    String[] arr = new string[] {
                                ocData.LineNumber.ToString(),
                                ocData.ItemNumber.ToString(),
                                lineDescExternal,
                                "<input "+estilo+" type=\"number\" value=\""+ orderedPurchaseQuantity +"\" max=\""+ orderedPurchaseQuantity +"\" min=\"1\" class=\"form-control canti\" oninput=\"if (Number(this.value) > Number(this.max)) this.value = this.max\" data-cantdyna=\""+ocData.OrderedPurchaseQuantity.ToString()+"\" data-parcial=\""+parcial.ToString()+"\">",
                                ocData.PurchaseUnitSymbol,
                                "<label for=\"chkSeleccionar\" style=\"font-size:10px;\">Seleccionar</label><input type=\"checkbox\" class=\"form-control chkSinLote\" style=\"width:20px;height:20px;\" onclick=\"agregarSinLote('" + OC + "','" + ocData.LineNumber.ToString() + "','" + ocData.ItemNumber.ToString() + "','" + revision + "',this,'check')\"/>",
                                sinLote,
                                LineDesc
                            };
                    if (!noIngresada)
                    {
                        ocLinea.Add(arr);
                        parcial = false;
                    }
                }
            }
            else
            {
                String[] arr = new string[] { "SinResultados", "SinResultados", "SinResultados", "SinResultados", "SinResultados" };
                ocLinea.Add(arr);
            }
            return ocLinea;
        }

        public async Task<object> SaveDataOC(OrdenCompraHeader header, List<OrdenCompraLinea> lines, String nombre)
        {
            SqlDataReader reader = null;
            try
            {
                /////////////////////////insercio o actualizacion de la cabecera////////////////////////////////////////////////////////////////
                String queryInsHead = "EXECUTE AYT_SetOrdenCompraHeader '" + header.PurchaseOrderNumber + "', '" + header.PurchaseOrderName + "', '" + DateTime.Parse(header.RequestedDeliveryDate).ToString("yyyy-MM-dd") + "', '" + header.BuyerGroupId + "', '" + header.CurrencyCode + "', '" + header.DefaultReceivingSiteId + "', '" + header.DefaultReceivingWarehouseId + "', '" + nombre + "' , 1;";
                
                if (!config.Equals("DESARROLLO"))
                {
                    reader = (SqlDataReader)await this.dbayt03.Query(queryInsHead);
                }
                else
                {
                    reader = (SqlDataReader)await this.dbprod.Query(queryInsHead);
                }
                int rowCount = 0;
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        rowCount = Convert.ToInt32(reader[0].ToString());
                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }
                reader.Close();
                String msg;
                if (rowCount > 0)
                {
                    msg = "Operacion en cabecera Exitosa!.";
                }
                else
                {
                    msg = "Ocurrio un error en la operacion de la cabecera!.";
                }
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////insercion o edicion de lineas////////////////////////////////////////////////////////////
                int rowCountLine = 0;
                bool errorline = false;
                object errordet = new { };
                Double revLinea = 0;
                foreach (OrdenCompraLinea linea in lines)
                {
                    revLinea = await Revisiones.GetRevision(linea.PurchaseOrderNumber);
                    String queryInsLineas = "EXECUTE AYT_SetOrdenCompraLineas '" + linea.LineNumber + "','" + linea.PurchaseOrderNumber + "','" + linea.ItemNumber + "','" + linea.LineDescription.Replace("'","ft") + "','" + linea.OrderedPurchaseQuantity + "','" + linea.PurchaseUnitSymbol + "','" + nombre + "','" + linea.cantdyna + "',0," + revLinea.ToString() + "," + linea.sinLote + ", '" + linea.lineDescriptionATP + "', '"+ linea.series +"';";
                    SqlDataReader readerLine;
                    if (config != "DESARROLLO")
                    {
                        readerLine = (SqlDataReader)await dbayt03.Query(queryInsLineas);
                    }
                    else
                    {
                        readerLine = (SqlDataReader)await this.dbprod.Query(queryInsLineas);
                    }
                    if (readerLine.HasRows)
                    {
                        while (readerLine.Read())
                        {
                            rowCountLine = Convert.ToInt32(readerLine[0].ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                    }
                    readerLine.Close();
                    if (rowCountLine == 0)
                    {
                        errorline = true;
                        errordet = new { lineNumber = linea.LineNumber, purchaseOrderNumber = linea.PurchaseOrderNumber, itemNumber = linea.ItemNumber };
                    }
                    rowCountLine += rowCountLine;
                }
                String msgLine;
                if (rowCountLine > 0)
                {
                    msgLine = "Operacion en lineas Exitosa!.";
                    await Revisiones.SetRevision(header.PurchaseOrderNumber);
                }
                else
                {
                    msgLine = "Ocurrio un error en la operacion de lineas!.";
                }
                return new { mensajeCab = msg, mensajeLine = msgLine, status = rowCount, errorLinea = errorline, detalleError = errordet, revision = revLinea.ToString(), purchaseOrderNumber = header.PurchaseOrderNumber };
            }
            catch (Exception e)
            {
                return e.Message.ToString();
            }
        }

        public async Task<ResultTransform> GetImagesPaths(String purchaseOrderNumber, String lineNumber, String itemNumber, String oc, int revision)
        {
            String query = "SELECT CONCAT(path,'/',nombreArchivo) AS path, nombreArchivo, comentario " +
                           "FROM " +
                           " AYT_CodigosQRImagenesFiles " +
                           " WHERE purchaseOrderNumber = '" + purchaseOrderNumber + "' " +
                           " AND lineNumber = '0' " +
                           " AND itemNumber = 'N/A'; ";
            SqlDataReader reader;
            if (config != "DESARROLLO")
            {
                reader = (SqlDataReader)await dbayt03.Query(query);
            }
            else
            {
                reader = (SqlDataReader)await dbprod.Query(query);
            }

            List<DataImgs> pathList = new List<DataImgs>();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    pathList.Add(new DataImgs() { path = reader[0].ToString(), nombre = reader[1].ToString(), comentario = reader[2].ToString() });
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            reader.Close();
            String queryComent = "SELECT * " +
                                 "FROM AYT_CodigosQRComentariosLote " +
                                 "WHERE purchaseOrderNumber = '" + oc + "' " +
                                 "AND lineNumber = '" + lineNumber + "' " +
                                 "AND itemNumber = '" + itemNumber + "'" +
                                 "AND revision = " + revision + 
                                 "ORDER BY revision DESC;";
            SqlDataReader readerComent;
            if (config != "DESARROLLO")
            {
                readerComent = (SqlDataReader)await dbayt03.Query(queryComent);
            }
            else
            {
                readerComent = (SqlDataReader)await dbprod.Query(queryComent);
            }
            List<DataLoteComentTransform> comentLote = new List<DataLoteComentTransform>();
            if (readerComent.HasRows)
            {
                while (readerComent.Read())
                {
                    comentLote.Add(new DataLoteComentTransform()
                                        { purchaseOrderNumber = oc,
                                          lineNumber = Convert.ToInt32(lineNumber),
                                          itemNumber = itemNumber, 
                                          lote = readerComent[5].ToString(), 
                                          loteATP = readerComent[7].ToString(), 
                                          comentario = readerComent[6].ToString(), 
                                          comentarioATP = readerComent[8].ToString(), 
                                          idComent = Convert.ToInt32(readerComent[0].ToString()),
                                          cantidad = Convert.ToDouble(readerComent[4].ToString()),
                                          serieATP = readerComent[9].ToString()
                                        });
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            readerComent.Close();
            ResultTransform result = new ResultTransform();
            result.data = pathList;
            result.dataLoteComent = comentLote;
            return result;
        }

        public async Task<object> SaveImagesComent(List<Data> DataLote, List<ComentImg> ComentImagen, DataOrd Orden, String Usuario, String Revision)
        {
            try
            {
                /////////////query de insercion de comentarios de lotes///////////////////////////////////////////////////////////////
                Boolean error = false;
                foreach (var comentLote in DataLote)
                {
                    String queryInsLote = "EXECUTE AYT_InsertComentariosLotesCodigosQR '" + Orden.purchaseOrderNumber + "','" + Orden.lineNumber.ToString() + "','" + Orden.itemNumber + "','" + comentLote.lote + "','" + comentLote.loteATP + "','" + comentLote.comentario + "','" + comentLote.comentarioATP + "','" + Usuario + "'," + Revision + ", '" + comentLote.cantidad.ToString() + "', '" + comentLote.serieATP + "';";
                    SqlDataReader reader;
                    if (config != "DESARROLLO")
                    {
                        reader = (SqlDataReader)await dbayt03.Query(queryInsLote);
                    }
                    else
                    {
                        reader = (SqlDataReader)await dbprod.Query(queryInsLote);
                    }
                    int rowCount = 0;
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            rowCount = Convert.ToInt32(reader[0].ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                        error = true;
                    }

                    if (rowCount == 0)
                    {
                        error = true;
                    }
                    reader.Close();
                }
                //////////////////insert de los comentarios de las imagenes/////////////////////////////////////////////////////////
                Boolean errorImg = false;
                foreach (var comentLImg in ComentImagen)
                {
                    String queryInsLote = "EXECUTE AYT_InsertComentariosImagenesCodigosQR '" + Orden.purchaseOrderNumber + "','" + Orden.lineNumber.ToString() + "','" + Orden.itemNumber + "','','" + comentLImg.comentario + "','" + Usuario + "','" + comentLImg.nombre + "';";
                    SqlDataReader readerImg;
                    if (config == "DESARROLLO")
                    {
                        readerImg = (SqlDataReader)await dbayt03.Query(queryInsLote);
                    }
                    else
                    {
                        readerImg = (SqlDataReader)await dbayt03.Query(queryInsLote);
                    }
                    int rowCount = 0;
                    if (readerImg.HasRows)
                    {
                        while (readerImg.Read())
                        {
                            rowCount = Convert.ToInt32(readerImg[1].ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                        errorImg = true;
                    }

                    if (rowCount == 0)
                    {
                        errorImg = true;
                    }
                    readerImg.Close();
                }
                String status = "Exito";
                if (error || errorImg)
                {
                    status = "Fallo";
                }
                return new { status, error = error.ToString(), errorImg = errorImg.ToString() };
            }
            catch (Exception ex)
            {
                return new { ex = ex.Message.ToString() };
            }
        }

        public async Task<object> RemoveLoteComent(DataLoteComent data)
        {
            try
            {
                String queryDelLote = "DELETE FROM AYT_CodigosQRComentariosLote WHERE idComent = '" + data.idcoment + "' AND purchaseOrderNumber = '" + data.purchaseordernumber + "' AND lineNumber = '" + data.linenumber + "' AND itemNumber = '" + data.itemnumber + "';";
                int readerComent;
                if (config != "DESARROLLO")
                {
                    readerComent = await dbayt03.queryInsert(queryDelLote);
                }
                else
                {
                    readerComent = await dbprod.queryInsert(queryDelLote);
                }
                var msg = "Fallo";
                if (readerComent > 0)
                {
                    msg = "Exito";
                }
                return new { data = msg };
            }
            catch (Exception ex)
            {
                return new { data = ex.Message.ToString() };
            }
        }

        public async Task<List<String[]>> GetDataOCVisor()
        {
            /////////////////////////insercio o actualizacion de la cabecera////////////////////////////////////////////////////////////////
            String queryInsHead = "  SELECT CONCAT(T0.purchaseOrderNumber,'-Rev',T1.revision) AS purchaseOrderNumber,T0.purchaseOrderName,T0.requestedDeliveryDate" +
                                  "  ,T0.buyerGroupId,T0.currencyCode,T0.defaultReceivingsiteId,T0.defaultReceivengWarehouseId, T1.recibido,T1.revision,T1.purchaseOrderNumber " +
                                  "  FROM AYT_CodigosQROrdenCompraCabecera T0 " +
                                  "  LEFT JOIN AYT_CodigosQROrdenCompraLineas T1 on(T1.purchaseOrderNumber = T0.purchaseOrderNumber) " +
                                  "  WHERE T0.status = 1 " +
                                  "  GROUP BY CONCAT(T0.purchaseOrderNumber,'-Rev',T1.revision), T0.purchaseOrderName,T0.requestedDeliveryDate,T0.buyerGroupId" +
                                  "  ,T0.currencyCode,T0.defaultReceivingsiteId,T0.defaultReceivengWarehouseId, T1.recibido,T1.revision,T1.purchaseOrderNumber; ";
            SqlDataReader reader;
            if (config != "DESARROLLO")
            {
                reader = (SqlDataReader)await dbayt03.Query(queryInsHead);
            }
            else
            {
                reader = (SqlDataReader)await dbprod.Query(queryInsHead);
            }
            List<String[]> ocHeader = new List<String[]>();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    String btnStatus = "";
                    switch (reader[7].ToString())
                    {
                        case "0":
                            btnStatus = "<span class=\"badge badge-warning\">Lineas Capturadas</span>";
                            break;
                        case "1":
                            btnStatus = "<span class=\"badge badge-info\">Lotes/Imagenes Cedis</span>";
                            break;
                        case "2":
                            btnStatus = "<span class=\"badge badge-success\">Lotes Capturados</span>";
                            break;
                    }
                    String[] ocH = new String[]
                            {
                                reader[0].ToString(),
                                reader[1].ToString(),
                                reader[2].ToString(),
                                reader[3].ToString(),
                                reader[4].ToString(),
                                reader[5].ToString(),
                                reader[6].ToString(),
                                btnStatus,
                                reader[8].ToString(),
                                reader[9].ToString()
                            };
                    ocHeader.Add(ocH);
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            reader.Close();
            return ocHeader;
        }

        public async Task<List<String[]>> GetDataOCLineaVisor(String OC, String itemNumber, String revision)
        {
            /////////////////////////insercio o actualizacion de la cabecera////////////////////////////////////////////////////////////////
            String queryLineas = "SELECT	T1.lineNumber " +
                                         ", T1.itemNumber " +
                                         ", T1.lineDescription " +
                                         ", T2.cantidad " +
                                         ", T1.purchaseUnitSymbol " +
                                         ", IIF(T2.loteATP LIKE 'LSD%', '', T2.loteATP) AS lote " +
                                         ", IIF (T2.comentarioATP LIKE 'CSD%', '', T2.comentarioATP) as comentario " +
                                         ", T1.revision " +
                                         ", T1.purchaseOrderNumber " +
                                         ", T2.serieATP " +
                                  " FROM AYT_CodigosQROrdenCompraLineas  T1 " +
                                  " LEFT JOIN AYT_CodigosQRComentariosLote T2 ON(T1.purchaseOrderNumber = T2.purchaseOrderNumber AND T1.lineNumber = T2.lineNumber AND T1.itemNumber = T2.itemNumber AND T2.revision = T1.revision) " +
                                  " WHERE T1.purchaseOrderNumber = '" + OC + "'" +
                                  " AND T1.recibido = 2" +
                                  " AND T2.revision = " + revision + "" +
                                  " ORDER BY 1;";
            SqlDataReader reader;
            if (config != "DESARROLLO")
            {
                reader = (SqlDataReader)await dbayt03.Query(queryLineas);
            }
            else
            {
                reader = (SqlDataReader)await dbprod.Query(queryLineas);
            }
            List<String[]> ocLineas = new List<String[]>();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    ///////generar cadena de imagen//////////////////////////////////////////////////////////////////////////////
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode((reader[5].ToString() + "-" + reader[8].ToString().Replace("-", "")), QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    using (Bitmap bitMap = qrCode.GetGraphic(20))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] imageBytes = ms.ToArray();
                            var imageStr = "";
                            if (!reader[5].ToString().Equals(""))
                            {
                                imageStr = "<img src=\"data:image/png;base64," + Convert.ToBase64String(imageBytes) + "\" style=\"width:75px;height:75px;\" />";
                            }
                            String pdf = "<i class=\"fa fa-file-pdf-o fa-2x\" style=\"cursor:pointer\" onclick=\"convertToPDF('" + OC + "', '" + reader[7].ToString() + "','" + reader[8].ToString().Replace("-", "") + "')\"></i>";
                            String loteATP = reader[5].ToString().Contains("-OC") ? reader[5].ToString() : (reader[5].ToString() + "-" + reader[8].ToString().Replace("-", ""));
                            String[] ocL = new String[]
                            {
                                reader[0].ToString(),
                                reader[1].ToString(),
                                reader[2].ToString(),
                                reader[3].ToString(),
                                reader[4].ToString(),
                                loteATP,
                                imageStr,
                                reader[6].ToString(),
                                reader[9].ToString(),
                                pdf
                            };
                            ocLineas.Add(ocL);
                        }
                    }
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////                    
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            reader.Close();
            return ocLineas;
        }

        public async Task<String> SendMail(String oc, String revision, String ocLote)
        {
            MemoryStream memoStream = new MemoryStream();
            SmtpClient client = new SmtpClient();
            NetworkCredential Credential = new NetworkCredential("notificaciones@avanceytec.com.mx", "avanceytec");
            MailMessage mail = new MailMessage();
            String body = "";
            String company;
            String cfdiCuenta;

            /////////////////////////insercion o actualizacion de la cabecera////////////////////////////////////////////////////////////////
            //String queryLineas = "SELECT	 T1.purchaseOrderNumber" +
            //                              ", T1.lineNumber " +
            //                              ", T1.itemNumber " +
            //                              ", T1.lineDescription " +
            //                              ", T2.cantidad " +
            //                              ", T1.purchaseUnitSymbol " +
            //                              ", IIF(T2.loteATP LIKE 'LSD%', '', T2.loteATP) AS lote " +
            //                              ", IIF (T2.comentarioATP LIKE 'CSD%', '', T2.comentarioATP) as comentario " +
            //                              ", T2.serieATP " +
            //                              ", T1.lineDescriptionATP " +
            //                      "FROM AYT_CodigosQROrdenCompraLineas  T1 " +
            //                      "LEFT JOIN AYT_CodigosQRComentariosLote T2 ON(T1.purchaseOrderNumber = T2.purchaseOrderNumber AND T1.lineNumber = T2.lineNumber AND T1.itemNumber = T2.itemNumber AND T1.revision = T2.revision) " +
            //                      "WHERE T1.purchaseOrderNumber = '" + oc + "'" +
            //                      "AND T1.recibido = 2" +
            //                      "AND T2.revision = " + revision + "" +
            //                      "ORDER BY 2;";
            //SqlDataReader reader;
            //if (config != "DESARROLLO")
            //{
            //    reader = (SqlDataReader)await dbayt03.Query(queryLineas);
            //}
            //else
            //{
            //    reader = (SqlDataReader)await dbprod.Query(queryLineas);
            //}

            //int count = 1;
            //WebClient wc = new WebClient();
            //if (reader.HasRows)
            //{
            //    wc.QueryString.Add("tipo", "resumenCedis");
            //    wc.QueryString.Add("ocLote", ocLote);
            //    while (reader.Read())

            //    {
            //        wc.QueryString.Add("data[" + count + "][0]", reader[0].ToString());
            //        wc.QueryString.Add("data[" + count + "][1]", reader[1].ToString());
            //        wc.QueryString.Add("data[" + count + "][2]", reader[2].ToString());
            //        wc.QueryString.Add("data[" + count + "][3]", reader[3].ToString());
            //        wc.QueryString.Add("data[" + count + "][4]", reader[4].ToString());
            //        wc.QueryString.Add("data[" + count + "][5]", reader[5].ToString());
            //        wc.QueryString.Add("data[" + count + "][6]", reader[6].ToString());
            //        wc.QueryString.Add("data[" + count + "][7]", reader[7].ToString());
            //        wc.QueryString.Add("data[" + count + "][8]", reader[8].ToString());
            //        wc.QueryString.Add("data[" + count + "][9]", reader[9].ToString());
            //        count++;                 
            //    }
            //}

            //reader.Close();
            //byte[] upload2 = null;
            //try
            //{
            //    MemoryStream stream2 = new MemoryStream();
            //    wc.Encoding = System.Text.Encoding.UTF8;
            //    upload2 = wc.UploadValues("http://inax.aytcloud.com/codigosQR/PDFCodigosQR.php", "POST", wc.QueryString);
            //    stream2.Write(upload2, 0, upload2.Length);
            //    stream2.Position = 0;
            //    StreamReader sr2 = new StreamReader(stream2);
            //    String str2 = sr2.ReadToEnd();
            //}catch(Exception es)
            //{
            //    Console.WriteLine(es.Message);
            //}

            try
            {
                company = "Avance y Tecnologia en Plasticos S.A. de C.V.";
                client.Port = 587;
                //client.Port = 465;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                //client.Host = "smtp.office365.com";
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Credentials = Credential;
                //cfdiCuenta = "CFDI";
                cfdiCuenta = "android";

                body = "<html><HEAD></HEAD>";
                body += "<BODY><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Consolas>&nbsp; ";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>A quien corresponda: </FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>&nbsp;</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri></FONT></SPAN>&nbsp;</P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Usted está<SPAN style=\"mso-spacerun: yes\">&nbsp; </SPAN>recibiendo una notificacion de una captura de lotes" + company + "</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Dicha captura se podrá<SPAN style=\"mso-spacerun: yes\">&nbsp; </SPAN>visualizar en PDF e imprimirlo libremente </FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Por parte del departamento de compras se envían lotes finales</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>&nbsp;</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><SPAN style=\"mso-spacerun: yes\"><FONT face=Calibri></FONT></SPAN></SPAN>&nbsp;</P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><SPAN style=\"mso-spacerun: yes\"><FONT face=Calibri></FONT></SPAN></SPAN>&nbsp;</P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Saludos cordiales. </FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>" + company + "</FONT></SPAN></P></FONT></SPAN></BODY></html>";
                mail.Subject = "Codigos QR de OC: " + oc;
                mail.Body = body;
                mail.IsBodyHtml = true;

                MailAddress fromATP = new MailAddress(cfdiCuenta + "@avanceytec.com.mx", "Notificaciones Avance");

                //mail.To.Add("sistemas9@avanceytec.com.mx");
                //mail.To.Add("sistemas12@avanceytec.com.mx");

                String correosQuery = "SELECT correos FROM AYT_ListaCorreos WHERE lista = 'CodigosQRCompras' OR lista = 'CodigosQRRecibos' AND status = 1;";
                SqlDataReader readerCorreos;
                if (config != "DESARROLLO")
                {
                    readerCorreos = (SqlDataReader)await dbayt03.Query(correosQuery);
                }
                else
                {
                    readerCorreos = (SqlDataReader)await dbprod.Query(correosQuery);
                }
                String correos = "";
                List<String[]> correosList = new List<String[]>();
                if (readerCorreos.HasRows)
                {
                    while (readerCorreos.Read())
                    {
                        correos = readerCorreos[0].ToString();
                        String[] correosArr = correos.Split(";");
                        correosList.Add(correosArr);
                    }
                }
                
                foreach (String[] corr in correosList)
                {
                    foreach(String mailStr in corr)
                    {
                        mail.To.Add(mailStr);
                    }
                    
                }
                readerCorreos.Close();
                //////get stream of pdf////////////////////////////////////////////////////////
                List<byte[]> bytePdfRecibos = await GetResponseBytesPDFVisor(oc, revision);
                //////////////////////////////////////////////////////////////////////////////
                if ((bytePdfRecibos != null) && (bytePdfRecibos.Count > 0))
                {
                    int count = 1;
                    String nameExt = "(Lotes_transformados).pdf";
                    foreach (byte[] pdfData in bytePdfRecibos)
                    {
                        mail.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(pdfData), oc + nameExt));
                        count++;
                        nameExt = "(Lotes_transformados) parte_" + count + ".pdf";
                    }
                }
                mail.From = fromATP;
                client.Send(mail);
                mail.Attachments.Clear();
                mail.To.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return ex.Message.ToString();
            }
            return "www";
        }

        public async Task<double> GetRevision(String OC)
        {
            var result = await Revisiones.GetRevision(OC);
            return result;
        }

        public async Task<List<OCList>> getListOC()
        {
            String queryListaOC = " SELECT CONCAT(T0.purchaseOrderNumber,'-',T1.revision) as purchaseOrderNumber,CONCAT(T0.purchaseOrderNumber,' - Rev ',T1.revision) as purchaseOrderNumberDesc,T1.revision " +
                                  " FROM AYT_CodigosQROrdenCompraCabecera T0 " +
                                  " INNER JOIN AYT_CodigosQROrdenCompraLineas T1 ON(T0.purchaseOrderNumber = T1.purchaseORderNumber) " +
                                  " WHERE T0.status = 1 " +
                                  " AND T1.recibido = 1" +
                                  " GROUP BY T0.purchaseOrderNumber,T1.revision; ";
            SqlDataReader readerOC;
            if (config != "DESARROLLO")
            {
                readerOC = (SqlDataReader)await dbayt03.Query(queryListaOC);
            }
            else
            {
                readerOC = (SqlDataReader)await dbprod.Query(queryListaOC);
            }
            List<OCList> ocList = new List<OCList>();
            if (readerOC.HasRows)
            {
                OCList listaTempOC1 = new OCList();
                listaTempOC1.text = "Seleccione una orden de compra....";
                listaTempOC1.value = "0";
                listaTempOC1.selected = true;
                listaTempOC1.revision = 0;
                ocList.Add(listaTempOC1);
                while (readerOC.Read())
                {
                    OCList listaTempOC = new OCList();
                    listaTempOC.text = readerOC[1].ToString();
                    listaTempOC.value = readerOC[0].ToString().Replace("OC-", "");
                    listaTempOC.selected = false;
                    listaTempOC.revision = Convert.ToDouble(readerOC[2].ToString());
                    ocList.Add(listaTempOC);
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            readerOC.Close();
            return ocList;
        }

        public async Task<String> SendMailRecibos(String oc, String revision)
        {
            MemoryStream memoStream = new MemoryStream();
            SmtpClient client = new SmtpClient();
            //private NetworkCredential Credential = new NetworkCredential("cfdi@facturasavance.com.mx", "@Soport3");
            NetworkCredential Credential = new NetworkCredential("notificaciones@avanceytec.com.mx", "avanceytec");
            MailMessage mail = new MailMessage();
            String body = "";
            String company;
            String cfdiCuenta;

            try
            {
                company = "Avance y Tecnologia en Plasticos S.A. de C.V.";
                client.Port = 587;
                //client.Port = 465;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                //client.Host = "smtp.office365.com";
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Credentials = Credential;
                //cfdiCuenta = "CFDI";
                cfdiCuenta = "android";

                body = "<html><HEAD></HEAD>";
                body += "<BODY><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Consolas>&nbsp; ";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>A quien corresponda: </FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>&nbsp;</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri></FONT></SPAN>&nbsp;</P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Usted está<SPAN style=\"mso-spacerun: yes\">&nbsp; </SPAN>recibiendo una notificacion de una captura de Orden de Compra : " + oc + "</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Por parte del departamento de compras se envían las claves a recibir para su captura de imagenes.</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>El archivo adjunto (si se presenta) contiene los articulos que no tienen numero de lote.</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>&nbsp;</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><SPAN style=\"mso-spacerun: yes\"><FONT face=Calibri></FONT></SPAN></SPAN>&nbsp;</P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><SPAN style=\"mso-spacerun: yes\"><FONT face=Calibri></FONT></SPAN></SPAN>&nbsp;</P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Saludos cordiales. </FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>" + company + "</FONT></SPAN></P></FONT></SPAN></BODY></html>";
                mail.Subject = "Codigos QR de OC: " + oc;
                mail.Body = body;
                mail.IsBodyHtml = true;

                MailAddress fromATP = new MailAddress(cfdiCuenta + "@avanceytec.com.mx", "Notificaciones Avance");

                String correosQuery = "SELECT correos FROM AYT_ListaCorreos WHERE lista = 'CodigosQRRecibos' AND status = 1;";
                SqlDataReader readerCorreos;
                if (config != "DESARROLLO")
                {
                    readerCorreos = (SqlDataReader)await dbayt03.Query(correosQuery);
                }
                else
                {
                    readerCorreos = (SqlDataReader)await dbprod.Query(correosQuery);
                }
                String correos = "";
                if (readerCorreos.HasRows)
                {
                    while (readerCorreos.Read())
                    {
                        correos = readerCorreos[0].ToString();
                    }
                }
                String[] correosArr = correos.Split(";");
                foreach (String corr in correosArr)
                {
                    mail.To.Add(corr);
                }
                //mail.To.Add("sistemas9@avanceytec.com.mx");
                //mail.To.Add("sistemas12@avanceytec.com.mx");
                readerCorreos.Close();
                //////get stream of pdf////////////////////////////////////////////////////////
                List<byte[]> bytePdfRecibos = await GetResponseBytesPDFRecibos(oc, revision);
                //////////////////////////////////////////////////////////////////////////////
                if ( (bytePdfRecibos != null) && (bytePdfRecibos.Count > 0) )
                {
                    int count = 1;
                    String nameExt = "(Sin_Lotes).pdf";
                    foreach (byte[] pdfData in bytePdfRecibos)
                    {
                        mail.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(pdfData), oc + nameExt));
                        count++;
                        nameExt = "(Sin_Lotes) parte_" + count + ".pdf";
                    }
                }
                mail.From = fromATP;
                client.Send(mail);
                mail.Attachments.Clear();
                mail.To.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return ex.Message.ToString();
            }

            return "qqq";
        }

        public async Task<List<byte[]>> GetResponseBytesPDFRecibos(String oc, String revision)
        {
            /////////////////////////insercion o actualizacion de la cabecera////////////////////////////////////////////////////////////////
            String queryLineas = "SELECT	 T1.purchaseOrderNumber " +
                                          ", T1.lineNumber " +
                                          ", T1.itemNumber " +
                                          ", T1.lineDescription " +
                                          ", T1.orderedPurchaseQuantity  " +
                                          ", T1.purchaseUnitSymbol " +
                                          ", IIF (T1.sinLote = 1, '', T2.loteATP) AS lote " +
                                          ", IIF (T1.sinLote = 1, '', T2.comentarioATP) as comentario " +
                                          ", T1.series as serieATP " +
                                          ", T1.lineDescriptionATP " +
                                  "FROM AYT_CodigosQROrdenCompraLineas  T1 " +
                                  "LEFT JOIN AYT_CodigosQRComentariosLote T2 ON(T1.purchaseOrderNumber = T2.purchaseOrderNumber AND T1.lineNumber = T2.lineNumber AND T1.itemNumber = T2.itemNumber AND T1.revision = T2.revision) " +
                                  "WHERE T1.purchaseOrderNumber = '" + oc + "' " +
                                  "AND T1.recibido = 0 " +
                                  "AND T1.revision = " + revision + " " +
                                  "ORDER BY 2;";
            SqlDataReader reader;
            if (config != "DESARROLLO")
            {
                reader = (SqlDataReader)await dbayt03.Query(queryLineas);
            }
            else
            {
                reader = (SqlDataReader)await dbprod.Query(queryLineas);
            }
            String ocLote = oc.Replace("-", "");
            int count = 1;
            WebClient wc = new WebClient();
            Boolean sinLotes = false;
            Boolean ultimosPorAgregar = false;
            List<WebClient> wcs = new List<WebClient>();
            if (reader.HasRows)
            {
                wc.QueryString.Add("tipo", "resumenCedisInicio");
                wc.QueryString.Add("ocLote", ocLote);
                sinLotes = true;
                while (reader.Read())
                {
                    ultimosPorAgregar = true;
                    wc.QueryString.Add("data[" + count + "][0]", reader[0].ToString());
                    wc.QueryString.Add("data[" + count + "][1]", reader[1].ToString());
                    wc.QueryString.Add("data[" + count + "][2]", reader[2].ToString());
                    wc.QueryString.Add("data[" + count + "][3]", reader[3].ToString());
                    wc.QueryString.Add("data[" + count + "][4]", reader[4].ToString());
                    wc.QueryString.Add("data[" + count + "][5]", reader[5].ToString());
                    wc.QueryString.Add("data[" + count + "][6]", reader[6].ToString());
                    wc.QueryString.Add("data[" + count + "][7]", reader[7].ToString());
                    wc.QueryString.Add("data[" + count + "][8]", reader[8].ToString());
                    wc.QueryString.Add("data[" + count + "][9]", reader[9].ToString());
                    count++;
                    if ( count % 20 == 0)
                    {
                        wcs.Add(wc);
                        wc = new WebClient();
                        wc.QueryString.Add("tipo", "resumenCedisInicio");
                        wc.QueryString.Add("ocLote", ocLote);
                        ultimosPorAgregar = false;
                    }
                }
                if (ultimosPorAgregar)
                {
                    wcs.Add(wc);
                }
            }

            reader.Close();
            byte[] upload2 = null;
            List<byte[]> uploadList = new List<byte[]>();
            try
            {
                MemoryStream stream2 = new MemoryStream();
                if (sinLotes)
                {
                    foreach (WebClient webCl in wcs)
                    {
                        webCl.Encoding = System.Text.Encoding.UTF8;
                        upload2 = wc.UploadValues(this.urlCodigosQRPDF, "POST", webCl.QueryString);
                        uploadList.Add(upload2);

                        stream2.Write(upload2, 0, upload2.Length);
                        stream2.Position = 0;
                        StreamReader sr2 = new StreamReader(stream2);
                        String str2 = sr2.ReadToEnd();
                        Console.WriteLine(str2);
                    }
                }
            }
            catch (Exception es)
            {
                Console.WriteLine(es.Message);
            }

            return uploadList;
        }

        public async Task<List<byte[]>> GetResponseBytesPDFVisor(String oc, String revision)
        {
            /////////////////////////insercion o actualizacion de la cabecera////////////////////////////////////////////////////////////////
            String queryLineas = "SELECT	 T1.purchaseOrderNumber" +
                                          ", T1.lineNumber " +
                                          ", T1.itemNumber " +
                                          ", T1.lineDescription " +
                                          ", T2.cantidad " +
                                          ", T1.purchaseUnitSymbol " +
                                          ", IIF(T2.loteATP LIKE 'LSD%', '', T2.loteATP) AS lote " +
                                          ", IIF (T2.comentarioATP LIKE 'CSD%', '', T2.comentarioATP) as comentario " +
                                          ", T2.serieATP " +
                                          ", T1.lineDescriptionATP " +
                                  "FROM AYT_CodigosQROrdenCompraLineas  T1 " +
                                  "LEFT JOIN AYT_CodigosQRComentariosLote T2 ON(T1.purchaseOrderNumber = T2.purchaseOrderNumber AND T1.lineNumber = T2.lineNumber AND T1.itemNumber = T2.itemNumber AND T1.revision = T2.revision) " +
                                  "WHERE T1.purchaseOrderNumber = '" + oc + "'" +
                                  "AND T1.recibido = 2" +
                                  "AND T2.revision = " + revision + "" +
                                  "ORDER BY 2;";
            SqlDataReader reader;
            if (config != "DESARROLLO")
            {
                reader = (SqlDataReader)await dbayt03.Query(queryLineas);
            }
            else
            {
                reader = (SqlDataReader)await dbprod.Query(queryLineas);
            }
            String ocLote = oc.Replace("-", "");
            int count = 1;
            WebClient wc = new WebClient();
            Boolean sinLotes = false;
            Boolean ultimosPorAgregar = false;
            List<WebClient> wcs = new List<WebClient>();
            if (reader.HasRows)
            {
                wc.QueryString.Add("tipo", "resumenCedis");
                wc.QueryString.Add("ocLote", ocLote);
                sinLotes = true;
                while (reader.Read())
                {
                    ultimosPorAgregar = true;
                    wc.QueryString.Add("data[" + count + "][0]", reader[0].ToString());
                    wc.QueryString.Add("data[" + count + "][1]", reader[1].ToString());
                    wc.QueryString.Add("data[" + count + "][2]", reader[2].ToString());
                    wc.QueryString.Add("data[" + count + "][3]", reader[3].ToString());
                    wc.QueryString.Add("data[" + count + "][4]", reader[4].ToString());
                    wc.QueryString.Add("data[" + count + "][5]", reader[5].ToString());
                    wc.QueryString.Add("data[" + count + "][6]", reader[6].ToString());
                    wc.QueryString.Add("data[" + count + "][7]", reader[7].ToString());
                    wc.QueryString.Add("data[" + count + "][8]", reader[8].ToString());
                    wc.QueryString.Add("data[" + count + "][9]", reader[9].ToString());
                    count++;
                    if (count % 10 == 0)
                    {
                        wcs.Add(wc);
                        wc = new WebClient();
                        wc.QueryString.Add("tipo", "resumenCedis");
                        wc.QueryString.Add("ocLote", ocLote);
                        ultimosPorAgregar = false;
                    }
                }
                if (ultimosPorAgregar)
                {
                    wcs.Add(wc);
                }
            }

            reader.Close();
            byte[] upload2 = null;
            List<byte[]> uploadList = new List<byte[]>();
            try
            {
                MemoryStream stream2 = new MemoryStream();
                if (sinLotes)
                {
                    foreach (WebClient webCl in wcs)
                    {
                        webCl.Encoding = System.Text.Encoding.UTF8;
                        upload2 = wc.UploadValues(this.urlCodigosQRPDF, "POST", webCl.QueryString);
                        uploadList.Add(upload2);

                        stream2.Write(upload2, 0, upload2.Length);
                        stream2.Position = 0;
                        StreamReader sr2 = new StreamReader(stream2);
                        String str2 = sr2.ReadToEnd();
                    }
                }
            }
            catch (Exception es)
            {
                Console.WriteLine(es.Message);
            }

            return uploadList;
        }
    }

    public class OrdenCompraHeader
    {
        public String etag { get; set; }
        public String PurchaseOrderNumber { get; set; }
        public String PurchaseOrderName { get; set; }
        public String RequestedDeliveryDate { get; set; }
        public String RequestedDeliveryDateOrig { get; set; }
        public String BuyerGroupId { get; set; }
        public String CurrencyCode { get; set; }
        public String DefaultReceivingWarehouseId { get; set; }
        public String DefaultReceivingSiteId { get; set; }
    }

    public class OrdenCompraHeaderVisor
    {
        public String PurchaseOrderNumber { get; set; }
        public String PurchaseOrderName { get; set; }
        public String RequestedDeliveryDate { get; set; }
        public String BuyerGroupId { get; set; }
        public String CurrencyCode { get; set; }
        public String DefaultReceivingWarehouseId { get; set; }
        public String DefaultReceivingSiteId { get; set; }
        public String ImgArray { get; set; }
    }

    public class OrdenCompraLinea
    {
        public String etag { get; set; }
        public String LineNumber { get; set; }
        public String PurchaseOrderNumber { get; set; }
        public String ItemNumber { get; set; }
        public String LineDescription { get; set; }
        public String OrderedPurchaseQuantity { get; set; }
        public String cantdyna { get; set; }
        public String PurchaseUnitSymbol { get; set; }
        public String Seleccionar { get; set; }
        public int sinLote { get; set; }
        public String lineDescriptionATP { get; set; }
        public String series { get; set; }
    }

    public class Data
    {
        public String lote { get; set; }
        public String comentario { get; set; }
        public String loteATP { get; set; }
        public String comentarioATP { get; set; }
        public double cantidad { get; set; }
        public String serieATP { get; set; }
    }

    public class ComentImg
    {
        public String comentario { get; set; }
        public String imagen { get; set; }
        public String nombre { get; set; }
    }

    public class DataOrd
    {
        public String purchaseOrderNumber { get; set; }
        public int lineNumber { get; set; }
        public String itemNumber { get; set; }
    }

    public class DataLoteComent
    {
        public String itemnumber { get; set; }
        public int linenumber { get; set; }
        public String purchaseordernumber { get; set; }
        public int idcoment { get; set; }
        public int index { get; set; }
    }

    public static class Revisiones
    {
        public static List<Revision> revisiones = new List<Revision>();
        public static async Task<Double> GetRevision(String purchaseOrderNumber)
        {
            try
            {
                var rev = revisiones.Find(oc => oc.purchaseOrderNumber.Contains(purchaseOrderNumber));
                Double result;
                if (rev == null)
                {
                    await Revisiones.SetRevision(purchaseOrderNumber);
                    var rev2 = revisiones.Find(oc => oc.purchaseOrderNumber.Contains(purchaseOrderNumber));
                    result = rev2.revision;
                    //if (rev2.revision > 1)
                    //{
                    //    result = rev2.revision + 1;
                    //}
                }
                else
                {
                    result = rev.revision;
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1.0;
            }
        }

        public async static Task<int> SetRevision(String purchaseOrderNumber)
        {
            DatabaseAYT03 dbayt03 = new DatabaseAYT03();
            Database dbprod = new Database();
            var configs = new GetConfigsData();
            var config = await configs.GetConfigurationData("Config");
            var company = await configs.GetConfigurationData("Company");
            var rev = revisiones.Find(oc => oc.purchaseOrderNumber.Contains(purchaseOrderNumber));
            if (rev == null)
            {
                String queryRevision = "IF EXISTS(SELECT revision FROM AYT_CodigosQROrdenCompraLineas WHERE purchaseOrderNumber = '" + purchaseOrderNumber+ "' )BEGIN SELECT MAX(revision) AS revision, 1 AS existe FROM AYT_CodigosQROrdenCompraLineas WHERE purchaseOrderNumber = '" + purchaseOrderNumber + "' END ELSE BEGIN SELECT 1 AS revision, 0 AS existe END;";
                SqlDataReader readerRev;
                if ( config != "DESARROLLO")
                {
                    readerRev = (SqlDataReader)await dbayt03.Query(queryRevision);
                }
                else
                {
                    readerRev = (SqlDataReader)await dbprod.Query(queryRevision);
                }
                double revision = 0;
                if (readerRev.HasRows)
                {
                    while (readerRev.Read())
                    {
                        revision =  Convert.ToDouble(readerRev[0].ToString());
                        String existe = readerRev[1].ToString();
                        if ( (revision > 1) || ( existe.Contains("1")) )
                        {
                            revision++;
                        }
                        revisiones.Add(new Revision() { purchaseOrderNumber = purchaseOrderNumber, revision = revision });
                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                    revisiones.Add(new Revision() { purchaseOrderNumber = purchaseOrderNumber, revision = 1 });
                }
                await readerRev.CloseAsync();
            }
            else
            {
                rev.revision++;
            }

            return 1;
        }
    }

    public class Revision
    {
        public String purchaseOrderNumber { get; set; }
        public Double revision { get; set; }
    }

    public class DataImgs
    {
        public String path { get; set; }
        public String nombre { get; set; }
        public String comentario { get; set; }
    }

    public class DataLoteComentTransform
    {
        public String purchaseOrderNumber { get; set; }
        public int lineNumber { get; set; }
        public String itemNumber { get; set; }
        public String lote { get; set; }
        public String loteATP { get; set; }
        public String comentario { get; set; }
        public String comentarioATP { get; set; }
        public int idComent { get; set; }
        public double cantidad { get; set; }
        public String serieATP { get; set; }
    }

    public class ResultTransform
    {
        public List<DataImgs> data { get; set; }
        public List<DataLoteComentTransform> dataLoteComent { get; set; }
    }

    public class PdfTableLinesTransform
    {
        public List<String[]> data { get; set; }
    }

    public class ResponseOrdenCompra
    {
        public String context { get; set; }
        public List<OrdenCompraHeader> value { get; set; }
    }
   
    public class ResponseLineasOC
    {
        public String context { get; set; }
        public List<OrdenCompraLinea> value { get; set; }
    }

    public class NombreExterno
    {
        public String VendorProductDescription { get; set; }
    }

    public class ResponseNombreExterno
    {
        public String context { get; set; }
        public List<NombreExterno> value { get; set; }
    }
}
