using CodigosQRComprasCEDIS_2._0.Helpers;
using CodigosQRComprasCEDIS_2._0.TestHelper;
using Microsoft.AspNetCore.Http;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using RestSharp;
using RestSharp.Authenticators;

namespace CodigosQRComprasCEDIS_2._0.Models
{
    public class Cedis
    {
        private DatabaseAYT03 dbayt03 = new DatabaseAYT03();
        private Database dbprod = new Database();
        public String config { get; set; }
        public String company { get; set; }
        public GetConfigsData configs { get; set; }
        public String urlRestClient { get; set; }
        public String urlCodigosQRPDF { get; set; }

        public Cedis()
        {
            ////////////Obtener datos de configuracion////////////////////////////////////////////////////////////////////////////////////
            this.configs = new GetConfigsData();
            this.config = configs.GetConfigurationData("Config").Result;
            this.company = configs.GetConfigurationData("Company").Result;
            if (this.company == "atp")
            {
                if (config == "DESARROLLO")
                    urlRestClient = this.configs.GetConfigurationData("URL_RESTCLIENT_TES").Result;
                else
                    urlRestClient = this.configs.GetConfigurationData("URL_RESTCLIENT_PROD").Result;
            }
        }

        public async Task<List<String[]>> GetDataOCLinea(String OC, String Revision)
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
            String itemNumber = "",  cant = "", recibido = "",hasSubRevision = "";
            List<bool> flagOrdenCompleta = new List<bool>();
            var packages = ocLineaResp.value;
            if (packages.Count() > 0)
            {
                foreach (var ocData in packages)
                {
                    String query = "SELECT status,itemNumber,orderedPurchaseQuantity,recibido,ISNULL(hasSubRevision,0) AS hasSubRevision FROM AYT_CodigosQROrdenCompraLineas WHERE purchaseOrderNumber = '" + OC + "' AND itemNumber = '" + ocData.ItemNumber.ToString() + "' AND ( recibido = 0 OR hasSubRevision = 1 ) AND revision = " + Revision + ";";
                    SqlDataReader reader;
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
                            hasSubRevision = reader[4].ToString();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                        noIngresada = true;
                    }
                    reader.Close();
                    switch (status)
                    {
                        case 1:
                            cant = ocData.OrderedPurchaseQuantity.ToString();
                            break;
                        case 2:
                            cant = orderedPurchaseQuantity.ToString();
                            break;
                        case 3:
                            cant = orderedPurchaseQuantity.ToString();
                            break;
                        default:
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
                    ///////////////traer la cantidad e la subrevision/////////////////
                    Double hasSubRevCant = 0;
                    if (hasSubRevision == "1")
                    {
                        String querySub = "SELECT orderedPurchaseQuantity FROM AYT_CodigosQRSubRevisionLinea WHERE lineNumber = "+ ocData.LineNumber.ToString() + " AND purchaseOrderNumber = '"+ OC +"' AND itemNumber = '"+ ocData.ItemNumber.ToString() + "' AND revision = "+ Revision +";";
                        SqlDataReader readerSub;
                        if (config != "DESARROLLO")
                        {
                            readerSub = (SqlDataReader)await dbayt03.Query(querySub);
                        }
                        else
                        {
                            readerSub = (SqlDataReader)await dbprod.Query(querySub);
                        }
                        if (readerSub.HasRows)
                        {
                            while (readerSub.Read())
                            {
                                try
                                {
                                    hasSubRevCant = Convert.ToDouble(readerSub[0].ToString());
                                }catch(Exception e)
                                {
                                    Console.WriteLine(e.ToString());
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No rows found.");
                        }
                        readerSub.Close();
                    }
                    cant = (Convert.ToDouble(cant) - hasSubRevCant).ToString();
                    ///////////////////////////////////////////////////////////////////
                    String LineDesc = ocData.LineDescription.Replace("'", "ft. ");
                    LineDesc = LineDesc.Replace("\"", "inch. ");
                    String imagenDrop = "<i class=\"fa fa-picture-o fa-2x\" style=\"cursor:pointer\" aria-hidden=\"true\" onclick=\"showModalImgs('" + OC + "','" + ocData.LineNumber.ToString() + "','" + ocData.ItemNumber.ToString() + "',this)\"></i><div style=\"border: solid 1px black; width:50%; text-align: center; cursor:pointer;\" onclick=\"showModalText('" + OC + "','" + ocData.LineNumber.ToString() + "','" + ocData.ItemNumber.ToString() + "','<b>" + ocData.ItemNumber.ToString() + "</b> " + LineDesc.Replace("\n", "") + "',this)\"><i class=\"fa fa-font fa-2x\" aria-hidden=\"true\"></i><i class=\"fa fa-italic fa-2x\" aria-hidden=\"true\"></i></div>";
                    String[] arr = new string[] {
                        ocData.LineNumber.ToString(),
                        ocData.ItemNumber.ToString(),
                        LineDesc,
                        "<div class=\"canti\">"+cant+"</div><div class=\"cantiSubRev\" style=\"display:none;\"><input type=\"number\" class=\"form-control\" style=\"width:75%\" min=\"1\" max=\""+cant+"\" oninput=\"if (Number(this.value) > Number(this.max)) this.value = this.max\" value=\""+cant+"\"></div>",
                        ocData.PurchaseUnitSymbol,
                        "<label for=\"chkSubRev\" style=\"font-size:10px;\">Seleccionar</label><input type=\"checkbox\"  class=\"form-control chkSubRev\" style=\"width:20px;height:20px;\" onclick=\"subrevisionLinea(this)\">",
                        LineDesc                        
                    };
                    if (!noIngresada)
                    {
                        ocLinea.Add(arr);
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

        public async Task<ResultImg> uploadFiles(IFormFile files, String purchaseOrderNumber, String revision, String purchaseOrderNumberRev, String nombre)
        {
            String fileName = "";
            String fileExt = "";
            long size = 0;
            var filePath = "";
            String extension = "";
            String image64 = "";
            String msg = "";
            String estatus = "";
            int idCodigos = 0;
            try
            {
                int numImg = await folioImg.getFolio(purchaseOrderNumberRev);
                size = files.Length;
                filePath = "/images";
                String[] fileExtTemp = files.FileName.Split('.');
                fileExt = fileExtTemp[1];
                fileName = "imagen" + numImg + purchaseOrderNumberRev + DateTime.Now.ToString("ddMMyyyy") + "." + fileExt;
                extension = files.ContentType;
                using (var ms = new MemoryStream())
                {
                    files.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    image64 = Convert.ToBase64String(fileBytes);
                }
                /////////////////////////////////////////////insercion o edicion de imagenes////////////////////////////////////////////////////////////
                int rowCountImgs = 0;
                String queryInsImages = "EXECUTE AYT_InsertImagesCodigosQR '" + purchaseOrderNumber + "','0','N/A','" + fileName + "','" + extension + "','" + size + "','" + image64 + "','" + filePath + "','" + nombre + "'," + revision + ",'" + purchaseOrderNumberRev + "';";
                SqlDataReader readerImgs;
                if (config != "DESARROLLO")
                {
                    readerImgs = (SqlDataReader)await dbayt03.Query(queryInsImages);
                }
                else
                {
                    readerImgs = (SqlDataReader)await dbprod.Query(queryInsImages);
                }
                if (readerImgs.HasRows)
                {
                    while (readerImgs.Read())
                    {
                        rowCountImgs = Convert.ToInt32(readerImgs[0].ToString());
                        estatus = readerImgs[1].ToString();
                        idCodigos = Convert.ToInt32(readerImgs[2].ToString());
                    }
                }
                else
                {
                    Console.WriteLine("No rows found.");
                }
                readerImgs.Close();

                if (rowCountImgs > 0)
                {
                    msg = "Imagen insertada Exitosa!.";
                    var fileStream = File.Create("wwwroot/images/" + fileName, (int)size);
                    await files.CopyToAsync(fileStream);
                    fileStream.Close();
                    folioImg.setFolio(purchaseOrderNumberRev);
                }
                else
                {
                    msg = "Ocurrio un error al insertar la imagen!.";
                }
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
            catch (Exception ex)
            {
                msg = "Ocurrio un error al insertar la imagen!." + ex.Message;
            }

            // process uploaded files
            // Don't rely on or trust the FileName property without validation.

            ResultImg result = new ResultImg();
            result.count = 1;
            result.size = size;
            result.filePath = filePath;
            result.msg = msg;
            result.estatus = estatus;
            result.idCodigosImagen = idCodigos;
            result.mockFile = new { type = extension, name = fileName, url = "/images", size, methodo = "FreshFile" };
            return result;
        }

        public async Task<List<mockFile>> getServerDocs(String purchaseOrderNumber, String lineNumber, String itemNumber)
        {
            /////////////////////consulta de datos de imagenes//////////////////////////////////////////////////
            String queryRecImages = " SELECT extension,nombreArchivo,tamaño,path,idCodigosImagen "
                                    + " FROM AYT_CodigosQRImagenesFiles "
                                    + " WHERE purchaseOrderNumber = '" + purchaseOrderNumber + "'; ";
            SqlDataReader readerImgs;
            if (config != "DESARROLLO")
            {
                readerImgs = (SqlDataReader)await dbayt03.Query(queryRecImages);
            }
            else
            {
                readerImgs = (SqlDataReader)await dbprod.Query(queryRecImages);
            }
            List<mockFile> mockList = new List<mockFile>();
            if (readerImgs.HasRows)
            {
                while (readerImgs.Read())
                {
                    mockFile mock = new mockFile();
                    mock.tipo = readerImgs[0].ToString();
                    mock.name = readerImgs[1].ToString();
                    mock.size = readerImgs[2].ToString();
                    mock.url = readerImgs[3].ToString();
                    mock.idCodigosImagen = Convert.ToInt32(readerImgs[4].ToString());
                    mock.method = "Recuperar";
                    mockList.Add(mock);
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            readerImgs.Close();
            ////////////////////////////////////////////////////////////////////////////////////////////////////
            return mockList;
        }

        public async Task<String> removeFiles(String purchaseOrderNumber, String lineNumber, String itemNumber, int idCodigosImagenes)
        {
            String queryRemImg = "DELETE " +
                "                 FROM AYT_CodigosQRImagenesFiles" +
                "                 WHERE purchaseOrderNumber = '" + purchaseOrderNumber + "'" +
                "                 AND lineNumber = '" + lineNumber + "'" +
                "                 AND itemNumber = '" + itemNumber + "'" +
                "                 AND idCodigosImagen = '" + idCodigosImagenes + "';";
            int rowsAffected;
            if (config != "DESARROLLO")
            {
                rowsAffected = await dbayt03.queryInsert(queryRemImg);
            }
            else
            {
                rowsAffected = await dbprod.queryInsert(queryRemImg);
            }
            if (rowsAffected > 0)
            {
                folioImg.setFolio(purchaseOrderNumber);
                return "Exito";
            }
            else
            {
                return "Fail";
            }
        }

        public async Task<List<OCList>> getListOC()
        {
            String queryListaOC = " SELECT CONCAT(T0.purchaseOrderNumber,'-',T1.revision) as purchaseOrderNumber,CONCAT(T0.purchaseOrderNumber,' - Rev ',T1.revision) as purchaseOrderNumberDesc,T1.revision " +
                                  " FROM AYT_CodigosQROrdenCompraCabecera T0 " +
                                  " INNER JOIN AYT_CodigosQROrdenCompraLineas T1 ON(T0.purchaseOrderNumber = T1.purchaseORderNumber) " +
                                  " WHERE T0.status = 1 " +
                                  " AND T1.recibido = 0" +
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

        public async Task<String> SendMailCompras(String oc, String revision)
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
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Usted está<SPAN style=\"mso-spacerun: yes\">&nbsp; </SPAN>recibiendo una notificacion de una captura de Lotes de proveedor por parte de cedis Orden de Compra : " + oc + "</FONT></SPAN></P>";
                body += "<P class=MsoPlainText style=\"MARGIN: 0cm 0cm 0pt\"><SPAN style=\"FONT-SIZE: 12pt\"><FONT face=Calibri>Por parte del departamento de Cedis se envían lotes para su transformación</FONT></SPAN></P>";
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
                String correosQuery = "SELECT correos FROM AYT_ListaCorreos WHERE lista = 'CodigosQRCompras' AND status = 1;";
                SqlDataReader readerCorreos;
                int readerUpd;
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
                readerCorreos.Close();
                //mail.Attachments.Add(new System.Net.Mail.Attachment(stream, oc + ".pdf"));
                mail.From = fromATP;
                client.Send(mail);
                mail.Attachments.Clear();
                mail.To.Clear();
                String resultUPD = await updateLineasQR(oc, revision);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return ex.Message.ToString();
            }

            return "qqq";
        }

        public async Task<String> updateLineasQR(String oc, String revision)
        {
            String updLineasQR = "UPDATE AYT_CodigosQROrdenCompraLineas SET recibido = 1 WHERE purchaseOrderNumber = '" + oc + "' AND revision = " + revision + " AND ( hasSubRevision != 1 OR hasSubRevision IS NULL ); ";
            String result;
            int reader;
            if (config != "DESARROLLO")
            {
                reader = await dbayt03.queryInsert(updLineasQR);
            }
            else
            {
                reader = await dbprod.queryInsert(updLineasQR);
            }
            if (reader > 0)
            {
                result = "Exito";
            }
            else
            {
                result = "fallo";
            }
            String updLineasSubR = "UPDATE AYT_CodigosQRSubRevisionLinea SET recibido = 1 WHERE purchaseOrderNumber = '" + oc + "' AND revision = " + revision + "; ";
            String resultSubR;
            int readerSubR;
            if (config != "DESARROLLO")
            {
                readerSubR = await dbayt03.queryInsert(updLineasSubR);
            }
            else
            {
                readerSubR = await dbprod.queryInsert(updLineasSubR);
            }
            if (readerSubR > 0)
            {
                resultSubR = "Exito";
            }
            else
            {
                resultSubR = "fallo";
            }
            return result;
        }

        public async Task<String> SetSubrevision(List<SubRevision> subRev, String revision)
        {
            String result = "";
            foreach (SubRevision sub in subRev)
            {
                String query = "EXECUTE AYT_CodigosQRSetSubRevision "+sub.lineNumber+",'"+sub.purchaseOrderNumber+"','"+sub.itemNumber+"',"+sub.orderedPurchaseQuantity+","+revision+";";
                int reader;
                if (config != "DESARROLLO")
                {
                    reader = await dbayt03.queryInsert(query);
                }
                else
                {
                    reader = await dbprod.queryInsert(query);
                }
                if (reader > 0)
                {
                    result = "Exito";
                }
                else
                {
                    result = "fallo";
                }
            }
            return result;
        }
    }

    public class OrdenCompraLineaCedis
    {
        public String LineNumber { get; set; }
        public String PurchaseOrderNumber { get; set; }
        public String ItemNumber { get; set; }
        public String LineDescription { get; set; }
        public String OrderedPurchaseQuantity { get; set; }
        public String cantdyna { get; set; }
        public String PurchaseUnitSymbol { get; set; }
        public String Seleccionar { get; set; }

    }

    public class ResultImg
    {
        public int count { get; set; }
        public long size { get; set; }
        public String filePath { get; set; }
        public String msg { get; set; }
        public String estatus { get; set; }
        public object mockFile { get; set; }
        public int idCodigosImagen { get; set; }
    }

    public class mockFile
    {
        public String tipo { get; set; }
        public String name { get; set; }
        public String size { get; set; }
        public String url { get; set; }
        public int idCodigosImagen { get; set; }
        public String method { get; set; }
    }

    public class OCList
    {
        public String text { get; set; }
        public String value { get; set; }
        public bool selected { get; set; }
        public double revision { get; set; }
    }

    public static class folioImg
    {
        public static List<folio> folios;
        private static DatabaseAYT03 dbayt03 = new DatabaseAYT03();
        private static Database dbprod = new Database();

        public async static Task<int> getFolio(String purchaseOrderNumberRev)
        {
            if (folios == null)
            {
                folios = new List<folio>();
            }
            var folio = folios.Find(x=>x.purchaseOrderNumberRev == purchaseOrderNumberRev);
            if (folio != null)
            {
                int cuantasImg = await getCuantasImgsbyPurchaseOrder(purchaseOrderNumberRev);
                if (cuantasImg == 0)
                {
                    return 1;
                }
                return folio.folioNumber;
            }
            else
            {
                setFolio(purchaseOrderNumberRev);
                return 1;
            }
        }

        public async static void setFolio(String purchaseOrderNumberRev)
        {
            var folio = folios.Find(x => x.purchaseOrderNumberRev == purchaseOrderNumberRev);
            if (folio != null)
            {
                int cuantasImg = await getCuantasImgsbyPurchaseOrder(purchaseOrderNumberRev);
                if (cuantasImg > 0)
                {
                    folio.folioNumber = cuantasImg;
                }
                folio.folioNumber++;
            }
            else
            {
                int cuantasImg = await getCuantasImgsbyPurchaseOrder(purchaseOrderNumberRev);
                folio folioTemp = new folio();
                folioTemp.purchaseOrderNumberRev = purchaseOrderNumberRev;
                if (cuantasImg == 0)
                {
                    folioTemp.folioNumber = 1;
                }
                else
                {
                    folioTemp.folioNumber = cuantasImg + 1;
                }
                folios.Add(folioTemp);
            }
        }

        public static async Task<int> getCuantasImgsbyPurchaseOrder(String purchOrderNumberRev)
        {
            var configs = new GetConfigsData();
            var config = await configs.GetConfigurationData("Config");
            var company = await configs.GetConfigurationData("Company");

            String query = " SELECT TOP 1 SUBSTRING(NombreArchivo,7,( CHARINDEX('OC',NombreArchivo,0) - 7)) AS ImagenNumero " +
                           " FROM " +
                           " AYT_CodigosQRImagenesFiles " +
                           " WHERE purchaseOrderNumber = '" + purchOrderNumberRev + "'" +
                           " ORDER BY NombreArchivo DESC; ";
            SqlDataReader reader;
            if ( config != "DESARROLLO")
            {
                reader = (SqlDataReader)await folioImg. dbayt03.Query(query);
            }
            else
            {
                reader = (SqlDataReader)await dbprod.Query(query);
            }
            int cuantas = 0;
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    cuantas = Convert.ToInt32(reader[0].ToString());
                }
            }
            else
            {
                Console.WriteLine("No rows found.");
            }
            reader.Close();
            return cuantas;
        }
    }

    public class folio
    {
        public String purchaseOrderNumberRev { get; set; }
        public int folioNumber { get; set; }
    }

    public class SubRevision
    {
        public int lineNumber { get; set; }
        public String purchaseOrderNumber { get; set; }
        public String itemNumber { get; set; }
        public Double orderedPurchaseQuantity { get; set; }
    }
}
