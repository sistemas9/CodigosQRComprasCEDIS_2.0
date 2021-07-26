using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodigosQRComprasCEDIS_2._0.Helpers;
using CodigosQRComprasCEDIS_2._0.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;

namespace CodigosQRComprasCEDIS_2._0.Controllers
{
    public class CapturadeOCController : Controller
    {
        public IActionResult IndexAsync()
        {
            ViewBag.vista= "capturaOC";
            //LdapHelper ldap = new LdapHelper();
            //List<EntryAD> groups = ldap.SearchForGroup("OC");
            //ViewBag.AD = groups;
            return View();
        }

        public IActionResult VisorCodigosQR()
        {
            ViewBag.vista = "visor";
            return View();
        }

        public async Task<IActionResult> Carrusel(String purchaseOrderNumber)
        {
            ViewBag.vista = "carrusel";
            ResultTransform result = await GetImagesPathsAsync(purchaseOrderNumber, "0", "N/A",null,0);
            ViewBag.images = result.data;
            GetConfigsData configs = new GetConfigsData();
            String config = configs.GetConfigurationData("Config").Result;
            if (config != "DESARROLLO")
            {
                ViewBag.rutaImg = "https://ayt-apps.eastus.cloudapp.azure.com/CodigosQR";
            }
            else
            {
                //ViewBag.rutaImg = "https://localhost:6061/";
                ViewBag.rutaImg = "https://ayt-apps.eastus.cloudapp.azure.com/CodigosQRTest";
            }
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> GetDataOCAsync(String OC)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            ResponseOrdenCompra oc = await capturadeOC.GetDataOC(OC);
            List<String[]> ocLine = await capturadeOC.GetDataOCLinea(OC);
            return Json(new { data = ocLine, dataocHead = oc.value[0] });
        }

        [HttpPost]
        public async Task<JsonResult> GetDataOCCodigosAsync(String OC,String ocRevision, String revision)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            ResponseOrdenCompra oc = await capturadeOC.GetDataOC(OC);
            List<String[]> ocLine = await capturadeOC.GetDataOCLineaCodigos(OC, ocRevision, revision);
            return Json(new { data = ocLine, dataocHead = oc.value[0] });
        }

        [HttpPost]
        public async Task<JsonResult> SaveDataOCAsync(OrdenCompraHeader header, List<OrdenCompraLinea> lines)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            JsonResult response = Json(await capturadeOC.SaveDataOC(header, lines, User.Identity.Name));
            return response;
        }

        [HttpPost]
        public async Task<ResultTransform> GetImagesPathsAsync(String purchaseOrderNumber, String lineNumber, String itemNumber, String oc, int revision)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            ResultTransform result = await capturadeOC.GetImagesPaths(purchaseOrderNumber, lineNumber, itemNumber, oc, revision);
            return result;
        }

        public async Task<JsonResult> SaveImagesComentAsync(List<Data> DataLote, List<ComentImg> ComentImagen, DataOrd DataOrden, String Revision)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            var Usuario = User.Identity.Name;
            var result = await capturadeOC.SaveImagesComent(DataLote, ComentImagen,DataOrden,Usuario,Revision);
            return Json(result);
        }

        public async Task<JsonResult> RemoveLoteComentAsync(DataLoteComent data)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            var result = await capturadeOC.RemoveLoteComent(data);
            return Json(result);
        }

        public async Task<JsonResult> CargarDataOCVisorAsync()
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            List<String[]> oc = await capturadeOC.GetDataOCVisor();
            return Json(new { data = oc });
        }

        public async Task<JsonResult> GetDataLineasVisorAsync(String OC,String itemNumber, String revision)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            List<String[]> ocLine = await capturadeOC.GetDataOCLineaVisor(OC,itemNumber,revision);
            return Json(new { data = ocLine });
        }

        public FileResult ConvertToPDF(String html)
        {
            PdfTableLinesTransform res = Newtonsoft.Json.JsonConvert.DeserializeObject<PdfTableLinesTransform>(html);
            var arrayCompleto = res.data;

            int count = 1;
            WebClient wc = new WebClient();
            wc.QueryString.Add("tipo", "resumenVisor");
            foreach (String[] data in arrayCompleto)
            {
                wc.QueryString.Add("data[" + count + "][0]", data[0].ToString());
                wc.QueryString.Add("data[" + count + "][1]", data[1].ToString());
                wc.QueryString.Add("data[" + count + "][2]", data[2].ToString());
                wc.QueryString.Add("data[" + count + "][3]", data[3].ToString());
                wc.QueryString.Add("data[" + count + "][4]", data[4].ToString());
                wc.QueryString.Add("data[" + count + "][5]", data[5].ToString());
                wc.QueryString.Add("data[" + count + "][6]", data[5].ToString());
                wc.QueryString.Add("data[" + count + "][7]", data[7].ToString());
                wc.QueryString.Add("data[" + count + "][8]", data[8].ToString());
                count++;
            }
            MemoryStream stream2 = new MemoryStream();
            wc.Encoding = System.Text.Encoding.UTF8;
            var configs = new GetConfigsData();
            var urlCodigosQRPDF = configs.GetConfigurationData("URL_CODIGOSQR_PDF").Result;
            byte[] upload2 = wc.UploadValues(urlCodigosQRPDF, "POST", wc.QueryString);
            stream2.Write(upload2, 0, upload2.Length);
            stream2.Position = 0;
            StreamReader sr2 = new StreamReader(stream2);
            String str2 = sr2.ReadToEnd();
            return File(upload2, "application/pdf");
        }

        public IActionResult EnviarPDF(String oc, String revision, String ocLote)
        {
            CapturadeOC capturadeOC = new CapturadeOC();
            var result = capturadeOC.SendMail(oc,revision,ocLote);
            return Json(result);
        }

        public IActionResult SetRevision(String oc)
        {
            Revisiones.SetRevision(oc);
            return Json("Exito");
        }

        public async Task<ActionResult> GetListOCAsync()
        {
            CapturadeOC capturaOC = new CapturadeOC();
            List<OCList> ListaOc = await capturaOC.getListOC();
            return Json(new { data = ListaOc });
        }

        public async Task<JsonResult> SendMailRecibosAsync(String oc, String revision)
        {
            CapturadeOC captura = new CapturadeOC();
            var result = await captura.SendMailRecibos(oc, revision);
            return Json(result);
        }
    }
}